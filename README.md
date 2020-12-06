# Gossip-Protocol
Implementing Gossip Protocol and Push-Sum Protocol for Distributed Systems in F# using AKKA Actors
1) Types of Topology:
   a)Full
   b)2D Mesh
   c)Imperfect 2D Mesh
   d)Line 
2) a) Gossip Algorithm for all types of topology.
   b) Push-sum for all kinds of topology.
   c) Failure Model for Gossip-Algorithm 
   

3) a) Gossip
      i) Full : 10^5  (Takes 20 min to build topology and 25 min to converge)
      ii) 2D : 10^4
      iii)imp2D : 10^4
      iv) Line: 10^5 (Terminates relatively faster(~ 10 min) but convergence rate is poor(~50%))
   b) push-sum
       i)Full : 10^5 (Takes 20 min to build but converges very fast)
       ii)2D :  10^4
       iii) imp2D: 10^4
       iv) Line: 10^4 (Takes 1.5 hours to converge 10^5 took more than 2 hours but didnt converge)

4) Instructions to Run:
    On Windows use: dotnet fsi proj2.fsx 100 full gossip  (We didnt test this on Linux- Sometimes #r @ doesnt work)

