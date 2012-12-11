module FrameNode

open System
open System.Threading

type Data(value:int) =
    let value = value

    member this.getValue = value

type Node (name:string) = 
    let name = name

    let mutable i = 0

    let r = new Random()

    member this.generateData = fun ()-> 
                                        Thread.Sleep(TimeSpan.FromMilliseconds 100.0)
                                        i <- i + 1
                                        new Data(i)
    
    member this.processData (newData:Data) =                       
        if newData.getValue.ToString().Length >= 2 then
            Thread.Sleep(TimeSpan.FromSeconds (float(r.Next(0, 5))))
            Some(newData)
        else None 
              
                

        
    