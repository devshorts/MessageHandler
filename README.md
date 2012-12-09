MesageHandler
=============

This is an F# async messaging handler that leverages computational expressions and lightweight agents to enable asynchronous messaging passing into a message pump chain.

The idea here is you have a series of nodes hooked up like this:

Node1 -> Node2 -> Node3

This represents a message passing chain.  Each node has a `processData` function that takes a `Data` class. If it successfully processed the data it'll return a `Some` of a new `Data` class. If it didn't process the data and wants to end the chain, it will return `None`.  

The chain is executed using a `maybe monad`, so if any series of the chain returns `None` then the monadic binding will fail and the rest of the nodes won't execute.  

There is also a small wrapper on top of the `MailboxProcessor` class which lets us use the async computation expression to wait for posted events and then execute them.

Chains are started by creating an infinite sequence of data from the root node:

```
// generate an infinite sequence of our source data    
let rec sourceData (node:Node) = seq{
    yield node.generateData()
    Console.WriteLine("data generated")
    yield! sourceData node
} 
```

Which we can then pass to an infinite sequence pipe to post each data has it comes

```
let startChain rootNode = (sourceData rootNode) |> 
                            Seq.iter (fun data ->
                                             Console.WriteLine("posting data")
                                             messager.agent.Post data)
```

Starting the entire process we build another async expression and force it to run it it's own thread 

```
let chainEntryPoint = 
    async{
        do! Async.SwitchToNewThread()
        startChain node1
    }

Async.Start chainEntryPoint
```

In this case, `node1` is the root node (like in the chain example above).

When messages are posted to the agent, the agent will execute 

```
// process a chain instance function
member this.agent = MailboxProcessor.Start(fun item -> 
    let rec processLoop = 
        async{
                let! postedData = item.Receive()
                Console.WriteLine("received data")
                let result = this.executeChain postedData
                this.testResult result
                return! processLoop
        }
    processLoop)
```

Which will pass the posted data to a chain executor:

```
// If all nodes processed the chain
// we'll return bool option true, otherwise we'll return None
member private this.processNodesFunc pump initialData =   
        let rec processNodesFunc' pump initialData = 
                maybe{
                    match pump with 
                        | MessagePump.Consumer(i) -> 
                                        // got to the end so we can say we processed a chain
                                    let! consumerMessage = i.processData(initialData)
                                    return true
                        | MessagePump.Messager(curr, nextNode) -> 
                                    let! nextMessage = curr.processData initialData
                                    return! processNodesFunc' nextNode nextMessage
                }    
                                     
        processNodesFunc' pump initialData
```

This is where the `maybe` monad comes into play.  If any part of the chain fails to evaluate we can immediately return since the `maybe` monad's bind function will only execute the next statement if we return `Some` and not `None`.

The last function in the message consumer is to test the monadic chain result with a simple snippet:

```
// executes an async item, if the async returned Some we'll say the chain 
// succeeded
member private this.testResult ret = 
    if Option.isSome (ret) then 
        Console.WriteLine("chain succeeded")
    else
        Console.WriteLine("chain didn't succeed")

    Console.WriteLine()
```

Since the `processNodesFunc` execution will return a `bool option` if it processed everything or not            
                                                         