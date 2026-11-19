/// Othello num tabuleiro de L linhas por C colunas (as duas pares).
///
/// Comeca com quatro pecas no centro, as brancas numa diagonal e as pretas na
/// outra, e as pretas jogam primeiro. Jogar e por uma peca sua numa casa vazia de
/// modo a prender, numa linha reta (horizontal, vertical ou diagonal), uma ou mais
/// pecas do outro entre a nova e outra sua; todas as presas viram. Quem nao tem
/// jogada passa a vez. Quando nenhum dos dois tem, a partida acaba, e o placar e
/// a diferenca de pecas (as casas vazias nao vao para ninguem).
///
/// O tabuleiro e um par de mascaras de bits: a casa (l, c) e o bit l * C + c.
module Conde.Othello.Nucleo

open System.Collections.Generic
open System.Numerics

type Tabuleiro =
    { Linhas: int
      Colunas: int
      Cheio: uint64
      SemPrimeiraColuna: uint64
      SemUltimaColuna: uint64 }

    member t.Casas = t.Linhas * t.Colunas

let tabuleiro linhas colunas =
    if linhas < 2 || colunas < 2 || linhas % 2 = 1 || colunas % 2 = 1 then
        invalidArg "linhas" "linhas e colunas pares, de 2 em diante"
    if linhas * colunas > 32 then
        invalidArg "linhas" "ate 32 casas: as duas mascaras precisam caber juntas em 64 bits"
    let n = linhas * colunas
    let cheio = (1UL <<< n) - 1UL
    let mutable primeira = 0UL
    let mutable ultima = 0UL
    for l in 0 .. linhas - 1 do
        primeira <- primeira ||| (1UL <<< (l * colunas))
        ultima <- ultima ||| (1UL <<< (l * colunas + colunas - 1))
    { Linhas = linhas
      Colunas = colunas
      Cheio = cheio
      SemPrimeiraColuna = cheio &&& ~~~primeira
      SemUltimaColuna = cheio &&& ~~~ultima }

/// Desloca todas as pecas uma casa na direcao d (0..7), jogando fora o que sai
/// do tabuleiro. Os deslocamentos de lado precisam da mascara de coluna, senao a
/// peca da ultima coluna reaparece na primeira coluna da linha de baixo.
let inline desloca (t: Tabuleiro) (d: int) (b: uint64) : uint64 =
    let c = t.Colunas
    match d with
    | 0 -> (b <<< 1) &&& t.SemPrimeiraColuna // leste
    | 1 -> (b >>> 1) &&& t.SemUltimaColuna // oeste
    | 2 -> (b <<< c) &&& t.Cheio // sul
    | 3 -> b >>> c // norte
    | 4 -> (b <<< (c + 1)) &&& t.SemPrimeiraColuna &&& t.Cheio // sudeste
    | 5 -> (b <<< (c - 1)) &&& t.SemUltimaColuna &&& t.Cheio // sudoeste
    | 6 -> (b >>> (c - 1)) &&& t.SemPrimeiraColuna // nordeste
    | _ -> (b >>> (c + 1)) &&& t.SemUltimaColuna // noroeste

/// As casas onde `eu` posso jogar.
let jogadas (t: Tabuleiro) (eu: uint64) (outro: uint64) : uint64 =
    let vazias = t.Cheio &&& ~~~(eu ||| outro)
    let passos = max t.Linhas t.Colunas
    let mutable r = 0UL
    for d in 0 .. 7 do
        let mutable x = desloca t d eu &&& outro
        for _ in 1 .. passos do
            x <- x ||| (desloca t d x &&& outro)
        r <- r ||| (desloca t d x &&& vazias)
    r

/// As pecas que viram se `eu` jogar na casa `casa` (um bit so).
let viradas (t: Tabuleiro) (eu: uint64) (outro: uint64) (casa: uint64) : uint64 =
    let mutable r = 0UL
    for d in 0 .. 7 do
        let mutable f = 0UL
        let mutable x = desloca t d casa
        while x &&& outro <> 0UL do
            f <- f ||| x
            x <- desloca t d x
        if x &&& eu <> 0UL then r <- r ||| f
    r

/// A posicao de saida: (pretas, brancas). As pretas jogam primeiro.
let inicio (t: Tabuleiro) =
    let l = t.Linhas / 2
    let c = t.Colunas / 2
    let bit lin col = 1UL <<< (lin * t.Colunas + col)
    let brancas = bit (l - 1) (c - 1) ||| bit l c
    let pretas = bit (l - 1) c ||| bit l (c - 1)
    pretas, brancas

let pecas (b: uint64) = BitOperations.PopCount b

/// As casas de canto, para olhar primeiro.
let private cantos (t: Tabuleiro) =
    let bit lin col = 1UL <<< (lin * t.Colunas + col)
    bit 0 0 ||| bit 0 (t.Colunas - 1) ||| bit (t.Linhas - 1) 0 ||| bit (t.Linhas - 1) (t.Colunas - 1)

/// O motor: negamax com poda alfa-beta sobre a diferenca de pecas, com memoria de
/// intervalo [piso, teto] por posicao (as duas mascaras juntas numa chave de 64 bits).
///
/// As duas melhorias podem ser desligadas, para medir o que cada uma compra:
/// `ordenar` (a jogada que deixa o outro com menos jogadas primeiro) e
/// `janelaNula` (MTD(f) em vez de uma busca de janela inteira).
type Motor(t: Tabuleiro, ordenar: bool, janelaNula: bool) =
    let memoria = Dictionary<uint64, uint16>()
    let cantos = cantos t
    let mutable nos = 0L
    let n = t.Casas

    member _.Tabuleiro = t
    member _.Posicoes = memoria.Count
    member _.Nos = nos

    member this.Busca(eu: uint64, outro: uint64, alfa0: int, beta0: int) : int =
        nos <- nos + 1L
        let minhas = jogadas t eu outro
        if minhas = 0UL then
            if jogadas t outro eu = 0UL then pecas eu - pecas outro
            else -(this.Busca(outro, eu, -beta0, -alfa0))
        else
            let chave = eu ||| (outro <<< 32)
            let mutable piso = -n
            let mutable teto = n
            match memoria.TryGetValue chave with
            | true, v ->
                piso <- int (v &&& 0xFFus) - n
                teto <- int (v >>> 8) - n
            | _ -> ()
            if piso >= beta0 || piso = teto then piso
            elif teto <= alfa0 then teto
            else
                let alfaBusca = max alfa0 piso
                let betaBusca = min beta0 teto
                let mutable alfa = alfaBusca
                let mutable melhor = -n - 1
                // A ordem: primeiro a jogada que deixa o outro com MENOS jogadas
                // (cantos desempatam a favor). E a ordem que mais corta no fim do Othello.
                let filhos = ResizeArray<struct (int * uint64 * uint64)>()
                let mutable resto = minhas
                while resto <> 0UL do
                    let casa = resto &&& (0UL - resto)
                    resto <- resto ^^^ casa
                    let f = viradas t eu outro casa
                    let novoEu = outro ^^^ f
                    let novoOutro = eu ||| casa ||| f
                    let nota =
                        if ordenar then 2 * pecas (jogadas t novoEu novoOutro) - (if casa &&& cantos <> 0UL then 1 else 0)
                        else 0
                    filhos.Add(struct (nota, novoEu, novoOutro))
                if ordenar then filhos.Sort(fun (struct (a, _, _)) (struct (b, _, _)) -> compare a b)
                let mutable i = 0
                while i < filhos.Count && alfa < betaBusca do
                    let struct (_, novoEu, novoOutro) = filhos.[i]
                    let v = -(this.Busca(novoEu, novoOutro, -betaBusca, -alfa))
                    if v > melhor then melhor <- v
                    if v > alfa then alfa <- v
                    i <- i + 1
                if melhor <= alfaBusca then teto <- min teto melhor
                elif melhor >= betaBusca then piso <- max piso melhor
                else
                    piso <- melhor
                    teto <- melhor
                memoria.[chave] <- uint16 (piso + n) ||| (uint16 (teto + n) <<< 8)
                melhor

    /// O placar exato (minhas menos as do outro, no fim) para quem vai jogar.
    ///
    /// Em vez de uma busca de janela inteira, uma sequencia de buscas de janela
    /// nula (MTD(f)): cada uma so pergunta "e pelo menos b?", e a memoria de
    /// intervalo aproveita tudo de uma para a outra.
    member this.Valor(eu: uint64, outro: uint64) : int =
        if not janelaNula then
            this.Busca(eu, outro, -n - 1, n + 1)
        else
            let mutable g = 0
            let mutable piso = -n
            let mutable teto = n
            while piso < teto do
                let beta = if g = piso then g + 1 else g
                g <- this.Busca(eu, outro, beta - 1, beta)
                if g < beta then teto <- g else piso <- g
            g

    /// O placar das pretas na posicao de saida.
    member this.Abertura() : int =
        let p, b = inicio t
        this.Valor(p, b)

    /// Cada primeira jogada das pretas e o placar que ela da a elas: (linha, coluna, placar).
    member this.Primeiras() =
        let p, b = inicio t
        let js = jogadas t p b
        [ for i in 0 .. n - 1 do
              let casa = 1UL <<< i
              if js &&& casa <> 0UL then
                  let f = viradas t p b casa
                  yield i / t.Colunas, i % t.Colunas, -(this.Valor(b ^^^ f, p ||| casa ||| f)) ]

    /// A partida perfeita: em cada lance, a primeira jogada (na ordem das casas) que
    /// mantem o placar. Devolve as casas jogadas (-1 = passou) e o tabuleiro final.
    member this.PartidaPerfeita() =
        let p, b = inicio t
        let mutable eu = p
        let mutable outro = b
        let lances = ResizeArray<int>()
        let mutable acabou = false
        while not acabou do
            let js = jogadas t eu outro
            if js = 0UL then
                if jogadas t outro eu = 0UL then acabou <- true
                else
                    lances.Add -1
                    let x = eu
                    eu <- outro
                    outro <- x
            else
                let alvo = this.Valor(eu, outro)
                let mutable achou = false
                for i in 0 .. n - 1 do
                    let casa = 1UL <<< i
                    if not achou && js &&& casa <> 0UL then
                        let f = viradas t eu outro casa
                        if -(this.Valor(outro ^^^ f, eu ||| casa ||| f)) = alvo then
                            achou <- true
                            lances.Add i
                            let novoEu = outro ^^^ f
                            outro <- eu ||| casa ||| f
                            eu <- novoEu
        // No fim, quem jogou por ultimo e o "outro"; devolve (pretas, brancas).
        let pretasJogam = (lances.Count % 2 = 0)
        let pretas, brancas = if pretasJogam then eu, outro else outro, eu
        List.ofSeq lances, pretas, brancas

/// O placar das pretas em L x C e as posicoes guardadas.
let resolve linhas colunas =
    let m = Motor(tabuleiro linhas colunas, true, true)
    let v = m.Abertura()
    v, m.Posicoes
