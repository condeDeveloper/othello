/// As tabelas do README. Calculadas aqui, e em lugar nenhum mais.
module Conde.Othello.Medidas

open System.Collections.Concurrent
open Conde.Othello.Nucleo

let private guardadas = ConcurrentDictionary<string, obj>()

let private umaVez<'T> (chave: string) (f: unit -> 'T) : 'T =
    guardadas.GetOrAdd(chave, fun _ -> box (f ())) :?> 'T

/// Uma linha da tabela.
type Linha =
    { Linhas: int
      Colunas: int
      /// Pretas menos brancas no fim, com os dois jogando o melhor.
      Placar: int
      /// Cada primeira jogada das pretas: (linha, coluna, placar).
      Primeiras: (int * int * int) list
      /// A partida perfeita: as casas jogadas (-1 = passou) e as pecas no fim.
      Lances: int list
      Pretas: int
      Brancas: int
      Posicoes: int
      Nos: int64 }

let linha linhas colunas : Linha =
    umaVez (sprintf "l:%dx%d" linhas colunas) (fun () ->
        let m = Motor(tabuleiro linhas colunas, true, true)
        let v = m.Abertura()
        let posicoes = m.Posicoes
        let nos = m.Nos
        let lances, p, b = m.PartidaPerfeita()
        { Linhas = linhas
          Colunas = colunas
          Placar = v
          Primeiras = m.Primeiras()
          Lances = lances
          Pretas = pecas p
          Brancas = pecas b
          Posicoes = posicoes
          Nos = nos })

/// Os tabuleiros leves: duas linhas de 2 a 16 colunas, o 4x4, o 4x6 e o 6x4.
let leves =
    [ for c in 2 .. 2 .. 16 -> 2, c ] @ [ (4, 4); (4, 6); (6, 4) ]

/// O que cada melhoria compra: (placar, posicoes, nos) com cada combinacao
/// pedida de `ordenar` e `janelaNula`. (Sem ordenar, o 4x6 nao termina em 150 s,
/// entao no 4x6 so se mede com a ordem ligada.)
let cruzada linhas colunas (combinacoes: (bool * bool) list) =
    umaVez (sprintf "c:%dx%d:%A" linhas colunas combinacoes) (fun () ->
        [ for o, j in combinacoes do
              let m = Motor(tabuleiro linhas colunas, o, j)
              let v = m.Abertura()
              yield (o, j), (v, m.Posicoes, m.Nos) ])

let todas = [ (false, false); (true, false); (false, true); (true, true) ]
let comOrdem = [ (true, false); (true, true) ]

/// O juiz de grade contra o motor nos tabuleiros de ate 16 casas.
let juizGrade () =
    umaVez "grade" (fun () ->
        [ for l, c in [ (2, 2); (2, 4); (2, 6); (2, 8); (4, 4) ] -> (l, c), Juiz.placar l c, (linha l c).Placar ])
