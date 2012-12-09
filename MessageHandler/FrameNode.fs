module FrameNode

open System
open System.Threading

type Data(value:string) =
    let value = value

    member this.getValue = value

type Node (name:string) = 
    let name = name
    let r = new Random()

    member this.generateData = fun ()-> 
                                        Thread.Sleep(TimeSpan.FromSeconds 1.0)
                                        new Data(r.Next().ToString())
    
    member this.processData (newData:Data) =       
        Console.WriteLine("In node {0} with data {1}", name, newData.getValue)
        Thread.Sleep(TimeSpan.FromSeconds 1.0)
                
        let retVal = 
            if newData.getValue.Length >= 2 then 
                Some(newData)
            else None 
                                       
        retVal
                

        
    