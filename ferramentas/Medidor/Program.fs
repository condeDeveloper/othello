/// O medidor. Roda as tabelas e cospe os numeros que vao para o README.
///
///   dotnet run --project ferramentas/Medidor/Medidor.fsproj -c Release
open Conde.Othello.Medidas

let leve () =
    printfn "\n== o placar das pretas (pretas menos brancas no fim) =="
    for l, c in leves do
        let sw = System.Diagnostics.Stopwatch.StartNew()
        let x = linha l c
        printfn "   %dx%d: %+d  (%d a %d)  %d posicoes, %d nos  (%.2f s)" l c x.Placar x.Pretas x.Brancas x.Posicoes x.Nos sw.Elapsed.TotalSeconds
        printfn "      primeiras: %A" x.Primeiras
        printfn "      partida perfeita (%d lances): %A" x.Lances.Length x.Lances
    printfn "\n== o que cada melhoria compra: (placar, posicoes, nos) =="
    for (l, c), combinacoes in [ ((4, 4), todas); ((4, 6), comOrdem) ] do
        for (o, j), r in cruzada l c combinacoes do
            printfn "   %dx%d  ordenar=%-5b janelaNula=%-5b  %A" l c o j r
    printfn "\n== os juizes =="
    for (l, c), g, m in juizGrade () do
        printfn "   grade %dx%d: %+d, motor %+d" l c g m
    let a = linha 4 6
    let b = linha 6 4
    printfn "   transposto: 4x6 %+d, 6x4 %+d" a.Placar b.Placar
    let transpostas = b.Primeiras |> List.map (fun (l, c, v) -> c, l, v) |> List.sort
    printfn "   primeiras do 6x4 transpostas = primeiras do 4x6: %b" (transpostas = List.sort a.Primeiras)

[<EntryPoint>]
let main _ =
    leve ()
    0
