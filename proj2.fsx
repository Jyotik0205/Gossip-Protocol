#if INTERACTIVE
#r @"bin\MCD\Debug\netcoreapp3.1\Akka.dll"
//#r @"bin\MCD\Debug\netcoreapp3.1\Akka.Configuration.dll"
//#r @"bin\MCD\Debug\netcoreapp3.1\Fsharp.PowerPack.dll"
#r @"bin\MCD\Debug\netcoreapp3.1\Akka.Remote.dll"
#r @"bin\MCD\Debug\netcoreapp3.1\Akka.Fsharp.dll"


#endif

// module Main=
open System
open System.Threading
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
//Inputs
let mutable input1=fsi.CommandLineArgs.[1]|> int;
let mutable input2=fsi.CommandLineArgs.[2]|> string;
let mutable input3=fsi.CommandLineArgs.[3]|> string;
let mutable flag=false
let mutable flagt=false
let mutable running=0
if input1=0 || input1=1 then
    printfn "Please input more elements....you will not get right answer"
    
// let mutable input1=2000
// let input2="full"
// let input3="gossip"
let stopWatch = System.Diagnostics.Stopwatch.StartNew()
 // Utility Functions to check if the number is a perfect square 
let getnearestsquare n=Math.Sqrt((float)n)|>int|>fun n->n*n
// Message DataType for child Actors
let gossipdelay=10
let pushsumdelay=10
type CMessage=
   |Rumor of int*int
   |Print
   |CalcTopology
   |CalcTopologyLFull
   |SumRumor of float*float*int
   |Stop of int
type PMessage=
    |Start
    |Done of int*int
    |Build
    |Tdone
    |Started
type ACMessage=
    |Send of int
    |PushSum of float*float
 // Create Actor system using Akka  
let system=ActorSystem.Create("example3")
let rand=System.Random()
// let a=rand.Next(3,5)
// printf "Random is: %d" a
//A function to make model for child actors
let spawn_printer system name n=
  let mutable state: int=n                                                                                // Actor number
  let mutable gossipnumber: int=0                                                                         //Number of gossips received 
  let Topology = new System.Collections.Generic.List<int>()                                               //Topology of the actor 
  let mutable s=(float)n                                                                                  //s and w for pushsum                                                       
  let mutable w=1.0 
  let mutable c=0
  let mutable ifalive=true                                                                                //check if actor is still alive
  let childname=name+"child"                                                                              //Name of child 
  let childactor=spawn system childname<|
                     fun mailbox->
                        let rec loop()=
                            actor{
                               let! msg=mailbox.Receive()
                               match msg with
                               |Send k-> while ifalive do                                                 //For continously sending messages until convergence
                                       // printfn "%d INside loop" n 
                                                if Topology.Count=0 then 
                                                    ifalive<-false
                                                    running<-running-1
                                                    let parent=system.ActorSelection("akka://example3/user/parent")
                                                    parent.Tell(Done (state, gossipnumber))
                                                    return ()
                                                else
                                                    let mutable next=0
                                                    next<-rand.Next(0,Topology.Count)
                                                    next<-Topology.[next]
                                                    let nextneighbour=system.ActorSelection("akka://example3/user/parent/Actor"+(string(next)))
                                                 // printfn "Actor %d told Actor %d" n next
                                                    nextneighbour.Tell(Rumor (k, n)) 
                                                    for i=1 to gossipdelay do
                                                      Thread.Sleep(10)|>ignore
                                         return()
                               |PushSum (st,wt)->    while c<3 do
                                                      //printfn "In child of %d" n
                                                      if Topology.Count=0 then
                                                          //printfn "In child of %d and in if" n
                                                          c<-10
                                                          let parent=system.ActorSelection("akka://example3/user/parent")
                                                          parent.Tell(Done (state, c))
                                                          return ()
                                                      else
                                                          //printfn "In child of %d and in else" n
                                                          let mutable next=0
                                                          next<-rand.Next(0,Topology.Count)
                                                          next<-Topology.[next]
                                                          let nextneighbour=system.ActorSelection("akka://example3/user/parent/Actor"+(string(next)))
                                                          nextneighbour.Tell(SumRumor ((s/2.0) ,(w/2.0),n))
                                                          s<-s/2.0
                                                          w<-w/2.0
                                                          for i=1 to pushsumdelay do
                                                               Thread.Sleep(10)|>ignore
                                                     return()
                                   }
                        loop()
   //printfn "My name is: %s" name
  spawn system name<|
         fun mailbox->
             let rec loop()=
                actor{
                   let! msg=mailbox.Receive()
                   match msg with
                   |Rumor (k,source)-> if gossipnumber=0 then 
                                            gossipnumber<-gossipnumber+1
                                            //printfn "Actor %d has number of gossips: %d \n" n gossipnumber
                                            let child=system.ActorSelection("akka://example3/user/parent/Actor"+(string(n))+"child")
                                            child.Tell(Send k)
                                            let parent=system.ActorSelection("akka://example3/user/parent")
                                            parent.Tell(Started)
                                            running<-running+1
                                        else
                                            if gossipnumber<10 then
                                               gossipnumber<-gossipnumber+1
                                               //printfn "Actor %d has number of gossips: %d \n" n gossipnumber
                                               if gossipnumber=10 then
                                                   ifalive<-false
                                                   running<-running-1
                                                   let parent=system.ActorSelection("akka://example3/user/parent")
                                                   parent.Tell(Done (state,gossipnumber))
                                                   let sourcsender=system.ActorSelection("akka://example3/user/parent/Actor"+(string(source)))
                                                   sourcsender.Tell(Stop n) 
                                            elif ifalive=true then
                                                ifalive<-false
                                                let parent=system.ActorSelection("akka://example3/user/parent")
                                                parent.Tell(Done (state,gossipnumber))
                                            else let sourcsender=system.ActorSelection("akka://example3/user/parent/Actor"+(string(source)))
                               
                                                 sourcsender.Tell(Stop n) 

                                                 return()
                                      
                                  //printf "Still On"
                   |Print ->                printfn "From child %i : %i \n" n c 
                   |CalcTopology->          let inp=getnearestsquare input1
                                            let size=Math.Sqrt((float)inp)|>int
                                            if (n%size)<>0 then 
                                                Topology.Add(n+1)
                                            if (n-size)>0 then
                                               Topology.Add(n-size)
                                            if (n+size)<input1 then
                                                Topology.Add(n+size)
                                            if(n%size)<>1 then
                                               Topology.Add(n-1)

                                            if input2="imp2D" then
                                                let mutable next=0
                                                next<-rand.Next(1,inp+1)
                                                while next=n || Topology.Contains(next) do
                                                        next<-rand.Next(1,inp+1)
                                                Topology.Add(next)
                                            let parent=system.ActorSelection("akka://example3/user/parent")
                                            parent.Tell(Tdone)

                   |CalcTopologyLFull->     if input2="line" then
                                                 if n=1  then
                                                        Topology.Add(2)
                                                    elif n=input1 then
                                                        Topology.Add(input1-1)
                                                    else 
                                                        Topology.Add(n-1)
                                                        Topology.Add(n+1)
                                                     
                                             elif input2="full" then
                                                  for i in 1 .. input1 do
                                                    if n<>i then 
                                                        Topology.Add(i) 
                                            let parent=system.ActorSelection("akka://example3/user/parent")
                                            parent.Tell(Tdone)
                   |Stop k   ->             if Topology.Contains(k) then
                                                                Topology.Remove(k)|>ignore
                                              // printfn "Removed %d from topology of %d \n" k n
                                            return()
                                             
                   |SumRumor (s1,w1,source)->   if s=(float)n && w=1.0 then
                                                  
                                                
                                                  //printfn "%d started" n
                                                    running<-running+1
                                                    let parent=system.ActorSelection("akka://example3/user/parent")
                                                    parent.Tell(Started)
                                                if c<3 then
                                                             let r=s/w
                                                             s<-s+s1/2.0
                                                             w<-w+w1/2.0
                                                             let mutable sign=1.0
                                                             
                                                             //printfn "Ratio of %d before:%f  Ratio After:%f and C:%i and diff is %f:" n r (s/w) c (r-(s/w))
                                                             if (s/w)-r<0.0 then
                                                                sign<- -1.0
                                                             if sign*((s/w)-r)< Math.Pow(10.0,-10.0) then
                                                                c<-c+1
                                                             else c<-0
                                                            //  if r<>r then
                                                            //    printfn "Ratio of %d before:%f  Ratio After:%f and C:%i and diff is %f and s is %f and w is %f:" n r (s/w) c (r-(s/w)) s w
                                                            //    c<-5
                                                            //    let parent=system.ActorSelection("akka://example3/user/parent")
                                                            //    parent.Tell(Done (state,c))
                                                            //    let sourcsender=system.ActorSelection("akka://example3/user/parent/Actor"+(string(source)))
                                                            //    sourcsender.Tell(Stop n) 
                                                            //    return()
                                                             if Topology.Count=0 then
                                                                  //printfn "In child of %d and in if" n
                                                                      c<-6
                                                                      let parent=system.ActorSelection("akka://example3/user/parent")
                                                                      parent.Tell(Done (state, c))
                                                                      let sourcsender=system.ActorSelection("akka://example3/user/parent/Actor"+(string(source)))
                                                                      sourcsender.Tell(Stop n)
                                                                      sourcsender.Tell(SumRumor ((s1/2.0) ,(w1/2.0),n)) 
                                                                      running<-running-1
                                                                      //printfn "I have no one left to send"
                                                                      return ()
                                                             else
                                                                  //printfn "In child of %d and in else" n
                                                                      let mutable next=1
                                                                      next<-rand.Next(1,Topology.Count)
                                                                      next<-Topology.[next-1]
                                                                      let nextneighbour=system.ActorSelection("akka://example3/user/parent/Actor"+(string(next)))
                                                                     // printfn "Sending from %d to %d" n next 
                                                                      nextneighbour.Tell(SumRumor ((s/2.0) ,(w/2.0),n))
                                                                      s<-s/2.0
                                                                      w<-w/2.0
                                                             if c=3 then 
                                                               let parent=system.ActorSelection("akka://example3/user/parent")
                                                               parent.Tell(Done (state,c))
                                                               let sourcsender=system.ActorSelection("akka://example3/user/parent/Actor"+(string(source)))
                                                               sourcsender.Tell(Stop n) 
                                                               running<-running-1
                                                               //printfn "I am done with c=3 %d " n 
                                                       //return()
                                                elif  c>=3  then    let sourcsender=system.ActorSelection("akka://example3/user/parent/Actor"+(string(source)))
                                                                    sourcsender.Tell(Stop n) 
                                                                   // sourcsender.Tell(SumRumor ((s1/2.0) ,(w1/2.0),n)) 
                                                                    
                                                                    if Topology.Count=0 then
                                                                        //printfn "Here"
                                                                        let mutable next1=1
                                                                        next1<-rand.Next(1,input1)
                                                                        
                                                                        let nextneighbour=system.ActorSelection("akka://example3/user/parent/Actor"+(string(next1)))
                                                                        //printfn "Sending from %d to %d" n next1 
                                                                        nextneighbour.Tell(SumRumor ((s1/2.0) ,(w1/2.0),n))
                                                                    else
                                                                        let mutable next2=1
                                                                        next2<-rand.Next(1,Topology.Count)
                                                                        //printfn "Next%d " next2 
                                                                        
                                                                        next2<-Topology.[next2-1]
                                                                       // printfn "Sending from %d to %d" n next2 

                                                                        let nextneighbour=system.ActorSelection("akka://example3/user/parent/Actor"+(string(next2)))
                                                                        nextneighbour.Tell(SumRumor ((s1/2.0) ,(w1/2.0),source))
                                                                        nextneighbour.Tell(Stop n)
                                                                        Topology.Remove(next2)|>ignore
                                                                        //printfn "%d " n 
                   return! loop() 
                }
             loop()

//Parent Actor declaration  
          
let actor= spawn system "parent" <| fun mailbox->
               if(input2="2D" || input2="imp2D") then
                  let inp=getnearestsquare input1
                  let size=Math.Sqrt((float)inp)|>int
                  input1<-inp
               let Actor =                                                                   //Creating Actors according to the input 
                   [1..input1]
                      |> List.map(fun id-> spawn_printer mailbox ("Actor"+(string(id))) id)
               let mutable nActorsDone=0                                                     //Signifies number of actors that have terminated (without reaching convergence+converged)
               let mutable t=0                                                               
               let mutable converged=0                                                       //Signifies number of actors converged 
               let mutable started=0;

               if input2="2D"||input2="imp2D" then                                           //Building Topology 
                   for i=0 to input1-1 do
                    Actor.[i].Tell(CalcTopology)               
                else 
                    for i=0 to input1-1 do
                    Actor.[i].Tell(CalcTopologyLFull)
              
              
               let rec loop()=                                                         
                   actor{
                   let! msg=mailbox.Receive()
                  // printfn "From Parent %A" msg
                   match msg with
                   |Start->                                                                 //Start the process
                                                                                           
                                    if input3="push-sum" then
                                        Actor.[input1/2].Tell(SumRumor (0.0,0.0,1))
                                    else
                                        Actor.[input1/2].Tell(Rumor (3,0))
                                    return()
                           
                   
                          
                   |Build ->        if input2="2D"||input2="imp2D" then                     //Build Topology function 
                                       for i=0 to input1-1 do
                                        Actor.[i].Tell(CalcTopology)
                                    else 
                                        for i=0 to input1-1 do
                                        Actor.[i].Tell(CalcTopologyLFull)

                   |Done (n,k)->    nActorsDone<-nActorsDone+1                             //Convergence Controller
                                    //printfn "Ruuning:%i Started:%i" running started 
                                    if input3="gossip" then
                                            if k=10 then
                                             converged<-converged+1
                                             //printfn "I am Actor number %d and I am done with gossipnumber:%d and total %d actors are done\n" n k nActorsDone
                                    else
                                             if k=3 || k=5 || k=6 then
                                              converged<-converged+1
                                             // printfn "I am Actor number %d and I am done with s/w ratio:%d times and total %d actors are done\n" n k nActorsDone
                                    if(nActorsDone=input1) then
                                     //printfn "Actors convergeres= %d out of %d and reached: %d" converged input1 started
                                     flag<-true
                                    if started=nActorsDone && started<>0 && nActorsDone<>input1 && running<=0 then
                                      //printfn "Actors convergeres= %d out of %d but only able to reach %d actors with pinged actors %d" converged input1 started nActorsDone
                                      flag<-true

                    |Tdone ->     t<-t+1                                                   //For checking if topology building is done
                                  if t=input1 then
                                    flagt<-true

                    |Started ->  started<-started+1
                   return! loop()
                    

                   }
               loop()
          


//Creating ActorSelection of Parent
let parent=system.ActorSelection("akka://example3/user/parent")
//parent.Tell(Build)
printfn "Building Topologies....."
while flagt<>true do
 Async.Sleep(100)|>ignore
printfn "Starting Gossip Algorithm...."
#time
stopWatch.Start()
//printfn "Time Elapsed is: %i" stopWatch.ElapsedMilliseconds
let begintime=stopWatch.ElapsedMilliseconds
parent.Tell(Start)  //Start the Process
//Console.ReadLine()
let mutable q=true
while q do
 
 if flag 
  then q<-false
system.Stop(actor)
printfn "Ended and Time Taken to converge: %i ms\n" (stopWatch.ElapsedMilliseconds-begintime)
#time