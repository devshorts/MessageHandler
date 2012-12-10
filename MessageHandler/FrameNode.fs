module FrameNode

open System
open System.Threading

type Data(value:int) =
    let value = value

    member this.getValue = value

type Node (name:string) = 
    let name = name
    let r = new Random()

    let mutable i = 0
    member this.generateData = fun ()-> 
                                        Thread.Sleep(TimeSpan.FromMilliseconds 100.0)
                                        i <- i + 1
                                        new Data(i)
    
    member this.processData (newData:Data) =       
        Thread.Sleep(TimeSpan.FromSeconds 1.0)
                
        let retVal = 
            if newData.getValue.ToString().Length >= 2 then
                Some(newData)
            else None 
                                       
        retVal
                

        
    