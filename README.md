# Othello

Othello resolvido em tabuleiros pequenos, em **F#**, sem dependência nenhuma além do xUnit nos testes. Para cada tabuleiro de L linhas por C colunas (as duas pares, até 32 casas), o motor diz o placar final exato — pretas menos brancas — com os dois jogando o melhor possível.

Começa com quatro peças no centro, as brancas numa diagonal e as pretas na outra, e as pretas jogam primeiro. Jogar é pôr uma peça sua de modo a prender, numa linha reta, uma ou mais peças do outro entre a nova e outra sua; todas as presas viram. Quem não tem jogada passa. Quando nenhum dos dois tem, acabou. As casas vazias no fim não vão para ninguém.

---

## Por que F#

O tabuleiro é um par de máscaras `uint64`, e achar as jogadas é deslocar a máscara nas oito direções, com uma máscara de coluna nos deslocamentos de lado para a peça da última coluna não reaparecer na primeira coluna da linha de baixo. A busca é um `member` recursivo com a memória num `Dictionary`, as duas melhorias são dois `bool` no construtor do motor, e a tabela cruzada que mede cada uma é uma compreensão de lista.

---

## Os quatro achados

**1. No 4x4 as brancas ganham por 11 a 3, qualquer que seja a primeira jogada das pretas.** As quatro primeiras jogadas dão −8, e a partida perfeita tem 10 lances e deixa duas casas vazias.

**2. No 4x6 as pretas ganham por 20 a 4, mas só com duas das quatro primeiras jogadas.** As outras duas dão só +4. A partida perfeita enche o tabuleiro em 21 lances, com uma passada das brancas no meio.

**3. A ordem das jogadas é o que resolve o 4x6.** Jogar primeiro onde o outro fica com menos jogadas (os cantos desempatam) é a diferença entre 0,2 s e não terminar em 150 s. No 4x4, medido em tabela cruzada: sem melhoria nenhuma o motor guarda 6.339 posições; só a ordem, 448 (14,1 vezes menos); só a janela nula (MTD(f)), 2.812 (2,3 vezes menos); as duas, 339. No 4x6, com a ordem ligada, a janela nula corta de 118.241 para 31.454 posições (3,8 vezes).

**4. O mesmo jogo, transposto, custa 2,3 vezes mais.** O 6x4 é o 4x6 deitado, e o placar e as primeiras jogadas batem casa a casa. Mas o motor guarda 71.616 posições no 6x4 contra 31.454 no 4x6, porque a ordem das casas nos bits decide o desempate entre jogadas igualmente boas.

---

## A tabela

| tabuleiro | placar das pretas | no fim | lances da partida perfeita | posições guardadas |
|---|---:|---|---:|---:|
| 2x2 | 0 | 2 a 2 | 0 | 0 |
| 2x4 a 2x16 | 0 | 3 a 3 | 2 | 3 |
| 4x4 | −8 | 3 a 11 | 10 | 339 |
| 4x6 | +16 | 20 a 4 | 21 | 31.454 |
| 6x4 | +16 | 20 a 4 | 21 | 71.616 |

Em duas linhas, de 2x4 a 2x16, a partida perfeita acaba no segundo lance, 3 a 3, e o resto do tabuleiro fica vazio. O 4x8 passou de 10 minutos sem terminar e eu parei; o 6x6 tem 36 casas e não cabe na chave de 64 bits do motor. Nenhum dos dois está na tabela.

Cada número está preso num teste em `MedidasTestes.fs`, e a tabela cruzada do 4x6 em `PesadosTestes.fs`.

---

## Os juízes

**A grade ingênua.** Um Othello escrito sem máscara de bits: uma grade `int[,]`, as peças presas achadas andando casa a casa nas oito direções, minimax completo sem poda e sem ordem, memória pelo texto da grade. Concorda com o motor no 2x2, 2x4, 2x6, 2x8 e 4x4. No 4x6 ela não termina (veja abaixo), e quem confere o 4x6 é o juiz seguinte.

**O tabuleiro transposto.** O 6x4 tem de dar o mesmo placar do 4x6, e cada primeira jogada do 6x4, com linha e coluna trocadas, o mesmo valor da jogada correspondente do 4x6. Bate nas quatro. É o juiz que pega erro de máscara de coluna, porque num tabuleiro não quadrado um deslocamento errado só aparece num dos dois sentidos.

**As quatro combinações de melhoria.** Ordenar ou não, janela nula ou não: as quatro dão −8 no 4x4, e as duas com a ordem dão +16 no 4x6.

---

## O que as medidas me corrigiram

**A primeira versão não resolveu o 4x6.** Ela olhava os cantos primeiro e o resto na ordem das casas, com uma busca de janela inteira. Ficou minutos sem terminar, e eu parei. Troquei duas coisas de uma vez, a ordem pela mobilidade do outro e a janela nula, e o 4x6 saiu em 0,1 s. Como foram duas trocas juntas, medi depois cada uma separada, em tabela cruzada: quem resolve é a ordem (achado 3).

**A grade ingênua não alcança o 4x6.** Com a memória indexada pelo texto da grade, ela chegou perto de 2 GB em quatro minutos e eu parei. Troquei a chave por um número na base 3, e mesmo assim passou de 15 minutos e 2,6 GB sem terminar. Por isso ela confere até o 4x4, e o 4x6 fica com o juiz do tabuleiro transposto.

---

## Como usar

.NET 8 ou mais novo.

```bash
dotnet test testes/Othello.Testes/Othello.Testes.fsproj -c Release
dotnet run --project ferramentas/Medidor/Medidor.fsproj -c Release
dotnet fsi exemplos/uso.fsx
```

```fsharp
open Conde.Othello.Nucleo

let m = Motor(tabuleiro 4 6, true, true)
m.Abertura()     // 16
m.Primeiras()    // [(0, 2, 16); (1, 1, 4); (2, 4, 4); (3, 3, 16)]
```

## Os arquivos

- `src/Othello/Nucleo.fs` — as máscaras, as jogadas, as viradas e o motor.
- `src/Othello/Juiz.fs` — o Othello da grade ingênua.
- `src/Othello/Medidas.fs` — as tabelas do README, cada uma calculada uma vez só.
- `ferramentas/Medidor/Program.fs` — imprime as tabelas.
- `exemplos/uso.fsx` — o pacote em uso, sem compilar.
- `testes/` — 16 testes, com os números do README presos.

## Licença

MIT.
