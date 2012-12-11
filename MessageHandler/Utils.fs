module Utils

open System
open System.Threading

type ThreadUtil() = 
    member private this.createThread func = new Thread(new ThreadStart(func))

    member this.start func = (this.createThread func).Start()

let threadUtil = new ThreadUtil()