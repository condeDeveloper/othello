/// Os numeros do README, presos.
module Conde.Othello.MedidasTestes

open Xunit
open Conde.Othello.Medidas

[<Fact>]
let ``em duas linhas, de 2x4 a 2x16, a partida perfeita acaba em 2 lances, 3 a 3`` () =
    for c in 4 .. 2 .. 16 do
        let x = linha 2 c
        Assert.Equal(0, x.Placar)
        Assert.Equal(2, x.Lances.Length)
        Assert.Equal((3, 3), (x.Pretas, x.Brancas))

[<Fact>]
let ``4x4: as brancas ganham por 11 a 3, qualquer que seja a primeira jogada`` () =
    let x = linha 4 4
    Assert.Equal(-8, x.Placar)
    Assert.Equal((3, 11), (x.Pretas, x.Brancas))
    Assert.Equal(10, x.Lances.Length)
    Assert.Equal<(int * int * int) list>([ (0, 1, -8); (1, 0, -8); (2, 3, -8); (3, 2, -8) ], x.Primeiras)

[<Fact>]
let ``4x6: as pretas ganham por 20 a 4, e so duas das quatro primeiras jogadas dao isso`` () =
    let x = linha 4 6
    Assert.Equal(16, x.Placar)
    Assert.Equal((20, 4), (x.Pretas, x.Brancas))
    Assert.Equal(21, x.Lances.Length)
    Assert.Contains(-1, x.Lances) // as brancas passam uma vez
    Assert.Equal<(int * int * int) list>([ (0, 2, 16); (1, 1, 4); (2, 4, 4); (3, 3, 16) ], x.Primeiras)

[<Fact>]
let ``o mesmo jogo transposto custa 2,3 vezes mais posicoes`` () =
    Assert.Equal(31454, (linha 4 6).Posicoes)
    Assert.Equal(71616, (linha 6 4).Posicoes)
    Assert.Equal(339, (linha 4 4).Posicoes)

[<Fact>]
let ``a tabela cruzada das melhorias`` () =
    let esperado4 =
        [ (false, false), (-8, 6339, 10480L)
          (true, false), (-8, 448, 676L)
          (false, true), (-8, 2812, 4624L)
          (true, true), (-8, 339, 625L) ]
    Assert.Equal<((bool * bool) * (int * int * int64)) list>(esperado4, cruzada 4 4 todas)

[<Fact>]
let ``os juizes concordam`` () =
    for _, g, m in juizGrade () do
        Assert.Equal(g, m)
