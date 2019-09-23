# Практическая работа по основам Microsopft Orleans

Что нужно для подготовки:
1. Visual Studio 2019 Preview 4 - бесплатнгый варинат (https://docs.microsoft.com/en-us/visualstudio/releases/2019/release-notes-preview)
2. .NET Core 3.0 RC1 (https://dotnet.microsoft.com/download/dotnet-core/3.0)
3. Установить в Студию пакет сниппетов для Orleans: https://hz.pryaniky.com/azureday/snippets


## Структура Boilerplate'a:
 - OneBoxDeployment.OrleansUtilities - проект из примеров Orleans - содердит несколько классов для чтения конфигурации Silo из json-файла конфигурации
 - Pryaniky.OrleansHost - Консольный проект с запуском Silo
 - Pryaniky.Orleans.GrainInterfaces - пустой проект для интерфейсов грейнов
 - Pryaniky.Orleans.Grains  - пустой проект для размещения грейнов

## А теперь попробуем со всем этим взлететь
1. Создание и запуск простого грейна (User)
В Orleans есть две ключевые сущности:
Grain – виртуальный актор (реализует интервейс IGrain, IGrainWithXXXKey)
Silo – Хост, обслуживающий запуск Grain

Задача  - создать простой Grain “Пользователь” (User) и запустить его в Silo
Для этого в BoilerPlate проекта нужно доюбавить два проекта-библиотеки:
- GrainInterfaces
- Grains

В GrainInterfaces Добавлены два Nuget-пакета:
Orleans.Abstractions и Orleans.CodeGeneration

