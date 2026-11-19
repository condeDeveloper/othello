module Conde.Othello.JuizTestes

open Xunit
open Conde.Othello

[<Fact>]
let ``a grade ingenua concorda com o motor em todo tabuleiro de ate 16 casas`` () =
    for l, c in [ (2, 2); (2, 4); (2, 6); (2, 8); (4, 4) ] do
        Assert.True((Juiz.placar l c = fst (Nucleo.resolve l c)), sprintf "%dx%d" l c)

[<Fact>]
let ``a grade acha as pecas presas andando casa a casa`` () =
    let g = Juiz.gradeInicial 4 4
    Assert.Equal<(int * int) list>([ (1, 1) ], Juiz.presas g 1 0 1)
    Assert.Empty(Juiz.presas g 1 0 0)
    Assert.Empty(Juiz.presas g 1 1 1) // casa ocupada

[<Fact>]
let ``o 4x6 e o 6x4 sao o mesmo jogo transposto`` () =
    let m46 = Nucleo.Motor(Nucleo.tabuleiro 4 6, true, true)
    let m64 = Nucleo.Motor(Nucleo.tabuleiro 6 4, true, true)
    Assert.Equal(m46.Abertura(), m64.Abertura())
    let transpostas = m64.Primeiras() |> List.map (fun (l, c, v) -> c, l, v) |> List.sort
    Assert.Equal<(int * int * int) list>(List.sort (m46.Primeiras()), transpostas)
