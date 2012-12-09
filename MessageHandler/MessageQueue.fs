module MessageQueue
open System
open FrameNode

// data structure for message pump

type MessagePump = 
    | Consumer of Node
    | Messager of Node * MessagePump

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
    

type MessageHandler(chain:MessagePump) = 
    let chain = chain

    let maybe = MaybeBuilder()

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
                                                                        
    // curry the execution chain 
    member private this.executeChain data = this.processNodesFunc chain data

    // executes an async item, if the async returned Some we'll say the chain 
    // succeeded
    member private this.testResult ret = 
        if Option.isSome (ret) then 
            Console.WriteLine("chain succeeded")
        else
            Console.WriteLine("chain didn't succeed")

        Console.WriteLine()

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
