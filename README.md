# Extrator de Dados para Undertale (GameMaker)

Este projeto consiste em um **script para extração e organização de código-fonte** de jogos desenvolvidos com **GameMaker Studio (GMS)**, incluindo mods de *Undertale*.

⚠️ **Aviso importante**:  
Este script tem **finalidade exclusivamente educacional**, sendo destinado ao estudo da estrutura e funcionamento de projetos GameMaker.  
Utilize apenas em projetos próprios ou para fins de aprendizado.

---

## 📌 Objetivo

O objetivo do script é:
- Descompactar arquivos do jogo
- Organizar o código-fonte em **pastas por objeto**
- Separar automaticamente os eventos de cada objeto

Isso facilita o estudo da lógica e da arquitetura do projeto.

---

## 📁 Estrutura de saída

Os arquivos extraídos são organizados por **objeto**, e dentro de cada pasta os eventos são separados da seguinte forma:

- Destroy  
- Step  
- Draw  
- Alarm  
- Collision  
- Other  
- Keyboard  
- KeyPress  
- KeyRelease  
- Mouse  
- Gesture  
- Async  
- PreCreate  
- CleanUp  

---

## ▶️ Como usar

1. Baixe o **jogo ou mod de Undertale** que deseja estudar  
2. Abra o projeto no **GameMaker**
3. Vá até:
```bash
Scripts → Run Other Scripts
```
4. Selecione o arquivo:
```bash
extrator.csx
```
5. Aguarde a execução do script

Ao final do processo:
- Todos os arquivos extraídos serão listados no console
- O código será exportado para a pasta:


Export_Code

localizada no mesmo diretório do jogo

Sinta-se livre para adaptar o script conforme suas necessidades.

---

## ⚠️ Limitações

- Compatível principalmente com jogos feitos no **GameMaker Studio 1.4 / 1.8 ou inferiores**
- Compatibilidade **parcial** com algumas versões do **GameMaker Studio 2**
- Pode não funcionar corretamente em projetos mais recentes ou altamente ofuscados

---

## 📚 Observações finais

Este projeto **não tem fins comerciais** e não deve ser utilizado para redistribuição de código de terceiros.  
Seu uso é recomendado apenas para **estudo, aprendizado e análise técnica**.
