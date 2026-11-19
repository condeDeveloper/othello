/// O mais caro do que termina: o 4x6 com a busca de janela inteira (uns 3 s).
/// O CI roda num passo separado (filtro `FullyQualifiedName~Pesados`).
///
/// A grade ingenua no 4x6 NAO esta aqui: ela passou de 15 minutos e 2,6 GB sem
/// terminar, e foi parada. Ela confere ate o 4x4.
module Conde.Othello.PesadosTestes

open Xunit
open Conde.Othello.Medidas

[<Fact>]
let ``no 4x6, com a ordem ligada, a janela nula corta de 118.241 para 31.454 posicoes`` () =
    let esperado = [ (true, false), (16, 118241, 194778L); (true, true), (16, 31454, 49350L) ]
    Assert.Equal<((bool * bool) * (int * int * int64)) list>(esperado, cruzada 4 6 comOrdem)
