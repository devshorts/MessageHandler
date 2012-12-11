module Utils

open System
open System.Threading
open System.Threading.Tasks

type ThreadUtil() = 
    static let taskFactory = new TaskFactory()

    static member private createThread func = new Thread(new ThreadStart(func))

    static member start func = (ThreadUtil.createThread func).Start()

    static member startAsTask func = taskFactory.StartNew func

let threadUtil = new ThreadUtil()