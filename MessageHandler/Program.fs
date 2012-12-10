// Learn more about F# at http://fsharp.net

open System
open System.Diagnostics
open FrameNode
open MessageQueue

// source nodes

let node1 = new Node("node1")
let node2 = new Node("node2")
let node3 = new Node("node3")

let chain = MessagePump.Messager(node1, MessagePump.Messager(node2, MessagePump.Consumer(node3)))

// give our chain to the message handler. this will 
// handle the message pump queueing
let messager = new MessageHandler(chain)

// generate an infinite sequence of our source data    
let rec sourceData (node:Node) = seq{
    let data = node.generateData()
    
    if data.getValue < 20 then
        yield data
        yield! sourceData node
} 

// pipe our infinte sequence into the messager post
let startChain rootNode = (sourceData rootNode) |> 
                            Seq.iter (fun data ->
                                             Console.WriteLine("POSTING {0}", data.getValue.ToString())
                                             messager.queueData data
                                      )

let chainEntryPoint = 
    async{
        do! Async.SwitchToNewThread()
        startChain node1
    }

// event completed handler
let dataProcessed (data:Data) = 
    Console.WriteLine ("                COMPLETED: {0}", data.getValue)

// hook into event completed
messager.chainCompleted.Add(dataProcessed)

Async.Start chainEntryPoint

Console.WriteLine("done");

Console.ReadKey() |> ignore