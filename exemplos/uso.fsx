// Exemplo de uso, sem compilar nada:
//
//   dotnet fsi exemplos/uso.fsx
//
// Resolve o 4x4 e o 4x6 e mostra o que vale cada primeira jogada das pretas.
#load "../src/Othello/Nucleo.fs"

open Conde.Othello.Nucleo

for l, c in [ (4, 4); (4, 6) ] do
    let m = Motor(tabuleiro l c, true, true)
    printfn "%dx%d: as pretas terminam com %+d" l c (m.Abertura())
    for lin, col, v in m.Primeiras() do
        printfn "   jogando em (%d, %d): %+d" lin col v
