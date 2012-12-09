// Learn more about F# at http://fsharp.net

open System
open System.Diagnostics
open FrameNode
open MessageQueue

// maybe computation expression builder


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
    yield node.generateData()
    Console.WriteLine("data generated")
    yield! sourceData node
} 

let startChain rootNode = (sourceData rootNode) |> 
                            Seq.iter (fun data ->
                                             Console.WriteLine("posting data")
                                             messager.agent.Post data)

let chainEntryPoint = 
    async{
        do! Async.SwitchToNewThread()
        startChain node1
    }

Async.Start chainEntryPoint

Console.WriteLine("done");

Console.ReadKey() |> ignore