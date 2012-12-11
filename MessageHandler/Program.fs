// Learn more about F# at http://fsharp.net

open System
open System.Diagnostics
open FrameNode
open MessageQueue
open Utils

// source nodes

let node1 = new Node("node1")
let node2 = new Node("node2")
let node3 = new Node("node3")

let chain = MessagePump.Messager(node1, MessagePump.Messager(node2, MessagePump.EndConsumer(node3)))

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

// event completed handler
let dataProcessed (data:Data) = 
    Console.WriteLine ("                COMPLETED: {0}, Queue length {1}", data.getValue, messager.queuSize())

// hook into event completed
messager.chainCompleted.Add(dataProcessed)

Utils.ThreadUtil.start (fun () -> startChain node1)

Console.WriteLine("done");

Console.ReadKey() |> ignore