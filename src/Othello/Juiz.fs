/// O juiz: outro Othello, escrito do jeito mais literal possivel. Nada de mascara de
/// bits: uma grade int[,], e cada jogada procura as pecas presas andando casa a
/// casa nas oito direcoes. Minimax completo, sem poda e sem ordem de jogadas, com
/// memoria pela grade lida como um numero na base 3, mais quem joga.
module Conde.Othello.Juiz

open System.Collections.Generic

let private direcoes = [ (0, 1); (0, -1); (1, 0); (-1, 0); (1, 1); (1, -1); (-1, 1); (-1, -1) ]

/// As pecas que viram se `quem` jogar em (l, c). Lista vazia = jogada ilegal.
let presas (g: int[,]) quem l c =
    let linhas = Array2D.length1 g
    let colunas = Array2D.length2 g
    if g.[l, c] <> 0 then []
    else
        [ for dl, dc in direcoes do
              let mutable caminho = []
              let mutable x = l + dl
              let mutable y = c + dc
              while x >= 0 && x < linhas && y >= 0 && y < colunas && g.[x, y] = 3 - quem do
                  caminho <- (x, y) :: caminho
                  x <- x + dl
                  y <- y + dc
              if not caminho.IsEmpty && x >= 0 && x < linhas && y >= 0 && y < colunas && g.[x, y] = quem then
                  yield! caminho ]

let gradeInicial linhas colunas =
    let g = Array2D.zeroCreate linhas colunas
    let l = linhas / 2
    let c = colunas / 2
    g.[l - 1, c - 1] <- 2
    g.[l, c] <- 2
    g.[l - 1, c] <- 1
    g.[l, c - 1] <- 1
    g

/// O placar final, pretas (1) menos brancas (2), com os dois jogando o melhor.
let placar linhas colunas =
    if linhas * colunas > 38 then invalidArg "linhas" "a chave cabe em 64 bits so ate 38 casas"
    let memoria = Dictionary<int64, int>()
    let numero (g: int[,]) quem =
        let mutable k = int64 quem
        for l in 0 .. linhas - 1 do
            for c in 0 .. colunas - 1 do
                k <- k * 3L + int64 g.[l, c]
        k
    let rec valor (g: int[,]) quem =
        let k = numero g quem
        match memoria.TryGetValue k with
        | true, v -> v
        | _ ->
            let opcoes =
                [ for l in 0 .. linhas - 1 do
                      for c in 0 .. colunas - 1 do
                          let p = presas g quem l c
                          if not p.IsEmpty then yield l, c, p ]
            let v =
                if opcoes.IsEmpty then
                    let outroJoga =
                        [ for l in 0 .. linhas - 1 do for c in 0 .. colunas - 1 -> presas g (3 - quem) l c ]
                        |> List.exists (fun p -> not p.IsEmpty)
                    if outroJoga then valor g (3 - quem)
                    else
                        let mutable s = 0
                        g |> Array2D.iter (fun x -> if x = 1 then s <- s + 1 elif x = 2 then s <- s - 1)
                        s
                else
                    let resultados =
                        [ for l, c, p in opcoes ->
                              let h = Array2D.copy g
                              h.[l, c] <- quem
                              for x, y in p do h.[x, y] <- quem
                              valor h (3 - quem) ]
                    if quem = 1 then List.max resultados else List.min resultados
            memoria.[k] <- v
            v
    valor (gradeInicial linhas colunas) 1
