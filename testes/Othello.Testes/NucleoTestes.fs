module Conde.Othello.NucleoTestes

open System
open Xunit
open Conde.Othello.Nucleo

let t44 = tabuleiro 4 4
let t46 = tabuleiro 4 6
let bit (t: Tabuleiro) l c = 1UL <<< (l * t.Colunas + c)

[<Fact>]
let ``a saida do 4x4: brancas numa diagonal, pretas na outra`` () =
    let p, b = inicio t44
    Assert.Equal(bit t44 1 2 ||| bit t44 2 1, p)
    Assert.Equal(bit t44 1 1 ||| bit t44 2 2, b)

[<Fact>]
let ``na saida as pretas tem quatro jogadas, as de sempre`` () =
    let p, b = inicio t44
    Assert.Equal(bit t44 0 1 ||| bit t44 1 0 ||| bit t44 2 3 ||| bit t44 3 2, jogadas t44 p b)

[<Fact>]
let ``jogar prende e vira as pecas da linha`` () =
    let p, b = inicio t44
    // Preta em (0,1) prende a branca (1,1) contra a preta (2,1).
    Assert.Equal(bit t44 1 1, viradas t44 p b (bit t44 0 1))

[<Fact>]
let ``deslocar para o lado nao da a volta para a linha seguinte`` () =
    // Ultima coluna para leste: some. Primeira coluna para oeste: some.
    Assert.Equal(0UL, desloca t46 0 (bit t46 1 5))
    Assert.Equal(0UL, desloca t46 1 (bit t46 2 0))
    Assert.Equal(0UL, desloca t46 4 (bit t46 0 5)) // sudeste da ultima coluna
    Assert.Equal(0UL, desloca t46 7 (bit t46 2 0)) // noroeste da primeira coluna
    Assert.Equal(bit t46 2 3, desloca t46 4 (bit t46 1 2))

[<Fact>]
let ``so tabuleiros pares de ate 32 casas`` () =
    Assert.Throws<ArgumentException>(fun () -> tabuleiro 3 4 |> ignore) |> ignore
    Assert.Throws<ArgumentException>(fun () -> tabuleiro 6 6 |> ignore) |> ignore
    Assert.Equal(32, (tabuleiro 4 8).Casas)

[<Fact>]
let ``as quatro combinacoes de melhoria dao o mesmo placar no 4x4`` () =
    for o in [ false; true ] do
        for j in [ false; true ] do
            Assert.Equal(-8, Motor(t44, o, j).Abertura())
