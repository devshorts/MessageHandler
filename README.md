MesageHandler
=============

This is an F# async messaging handler that leverages computational expressions and lightweight agents to enable asynchronous messaging passing into a message pump chain.


===
Overview
===

The idea here is you have a series of nodes hooked up like this:

```
Node1 -> Node2 -> Node3
```
   
This represents a message passing chain.  Assuming `Node1` is the source node, we want this series to execute every time `Node1` has data to push.  We can represent the evaluation of an entire chain as a function and use a message queue to execute the function each time we receive data. 

The message queue will be asynchornous so even if pieces of the chain take a while to execute, it won't block the 
source node from pushing more data to the chain.

===
Details
===

Each node has a `processData` function that takes a `Data` class. If it successfully processed the data it'll return a `Some` of a new `Data` class. If it didn't process the data and wants to end the chain, it will return `None`.  

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
member private this.agent = 
    let syncContext = SynchronizationContext.Current

    MailboxProcessor.Start(fun item -> 
        let rec processLoop _ = 
            async{
                let! (postedData:Data) = item.Receive()
                Console.WriteLine("        RECEIVED: {0}", postedData.getValue.ToString())
                let result = this.executeChain postedData
                if this.testResult result then
                    chainCompletedEvent.Trigger (Option.get result)
                        
                return! processLoop()
            }
                                
        processLoop())
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

Since the `processNodesFunc` execution will return a `bool option` if it processed everything or not. If it passes then we dispatch an event to the `chainCompletedEvent` handler that we can catch elsewhere.

===
Maybe Monad
===
                                                         
The `maybe` monad is built using a classical computation expression definition like this:

```
type MaybeBuilder() =
    let bind value func =
        match value with
            | Some(x) -> func x
            | _ -> None

    let wrap value = Some(value)

    member this.Bind(x, f) = bind x f
    member this.Delay(f) = f()
    member this.Return(x) = wrap x
    member this.ReturnFrom(x) = x
    member this.Combine(a, b) = if Option.isSome a then a
                                else b 
    member this.Zero(a) = None
```
    
===
Output
===

The output should be pretty clear. We post data when we've generated a value from our sequence. The agent logs when it receives the value, and we hook into a completion event that is dispatched when the chain finally fires

```
done
POSTING 1
        RECEIVED: 1
POSTING 2
        RECEIVED: 2
POSTING 3
        RECEIVED: 3
POSTING 4
        RECEIVED: 4
POSTING 5
POSTING 6
POSTING 7
POSTING 8
POSTING 9
POSTING 10
        RECEIVED: 5
POSTING 11
        RECEIVED: 6
POSTING 12
        RECEIVED: 7
POSTING 13
        RECEIVED: 8
POSTING 14
        RECEIVED: 9
POSTING 15
POSTING 16
POSTING 17
POSTING 18
POSTING 19
        RECEIVED: 10
        RECEIVED: 11
        RECEIVED: 12
        RECEIVED: 13
        RECEIVED: 14
        RECEIVED: 15
        RECEIVED: 16
        RECEIVED: 17
        RECEIVED: 18
                COMPLETED: 10
        RECEIVED: 19
                COMPLETED: 11
                COMPLETED: 12
                COMPLETED: 13
                COMPLETED: 14
                COMPLETED: 15
                COMPLETED: 16
                COMPLETED: 17
                COMPLETED: 18
                COMPLETED: 19
```

A node will only process a message if the input value (the integer) is 2 digits or more, which is why we start getting completion messages after item 10. We also stopped generating the infinte sequence after 20 items just for demonstration.