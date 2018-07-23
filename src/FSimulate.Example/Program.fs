// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.
open System

[<EntryPoint>]
let main argv = 
    let mutable exitOnNextLoop = false
    let mutable skipLastCharacterExit = false

    while not exitOnNextLoop do
        //exitOnNextLoop <- true
        Console.WriteLine("Choose an example:")
        Console.WriteLine("      1:  Workshop with unreliable machines")
        Console.WriteLine("      2:  Call center")
        Console.WriteLine("      3:  Order delivery with warehouse reorder")
        Console.WriteLine("      4:  Alarm Clock")
        Console.WriteLine("      Q:  Quit")

        let keyInfo = Console.ReadKey(true)
        Console.WriteLine()

        match keyInfo.KeyChar.ToString().ToUpper() with
        | "1" -> Example1.run ()
        | "2" -> Example2.run ()
        | "3" -> Example3.run ()
        | "4" -> Example4.run ()
        | "Q" ->
            skipLastCharacterExit <- true
            exitOnNextLoop <- true
        | _ ->
            exitOnNextLoop <- false

    if not skipLastCharacterExit then
        Console.WriteLine()
        Console.WriteLine("Press any key...")
        Console.ReadKey() |> ignore

    0


