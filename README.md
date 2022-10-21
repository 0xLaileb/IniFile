<p align="center"> 
  <img align="center" src="https://github.com/0xLaileb/IniFile/blob/master/logo.png?raw=true" width="150"/> 
</p>

<h1><div align="center">IniFile</h1>
<p align="center">
  <img src="https://img.shields.io/badge/PRICE-free-%231DC8EE"/>
  <img src="https://img.shields.io/badge/SUPPORT-no-%231DC8EE"/>
</p>

<p align="center">
  <img src="https://img.shields.io/github/downloads/0xLaileb/IniFile/total?color=%231DC8EE&label=DOWNLOADS&logo=GitHub&logoColor=%231DC8EE&style=flat"/>
  <img src="https://img.shields.io/github/last-commit/0xLaileb/IniFile?color=%231DC8EE&label=LAST%20COMMIT&style=flat"/>
  <img src="https://img.shields.io/github/release-date/0xLaileb/IniFile?color=%231DC8EE&label=RELEASE%20DATE&style=flat"/>
</p>

<p align="center">
  Данный класс представляет возможность работы с <b>ini-файлами</b> на основе вызовов функций из <b>kernel32.dll</b>.<br>
  INI-файлы обычно применяются для сохранения параметров, которые используются при работе программы.<br>
  <b>Поддержка</b>: .Net Framework / .Net Core
</p>

[releases]: https://github.com/0xLaileb/IniFile/releases/

## 🔧 Основной функционал
- **Write** устанавливает строковые значения в ini-файлах. 
- **ReadString** читает строковые значения из ini-файлов.
- **ReadInt** читает числовое значение заданного ключа из ini-файла.
- **ReadBool** читает логическое значение заданного ключа из ini-файла.
- **GetAllDataSection** извлекает все ключи и значения для указанной секции файла инициализации.
- **GetAllSections** извлекает имена всех секций в файле инициализации.
- **DeleteKey** удаляет значение заданного ключа в определенной секции.
- **DeleteSection** удаляет заданную секцию.
- **KeyExists** производит чтение ключа по определенной секции и проверяет наличие значения.

## 🚀 Как использовать

- ### Инициализация класса 
1. Скачайте последний релиз : **[Releases][releases]**.
2. Добавьте файл `IniFile.cs` в свой проект.
3. Инициализируйте класс: 
```csharp
IniFile iniFile = new IniFile("file_name.ini");
```

- ### Примеры использования
1. Запись строкового значения ключа:
```csharp
iniFile.Write("KEY", "value", "SECTION");
```
2. Чтение строкового значения ключа (<b>return: string</b>):
```csharp
iniFile.ReadString("KEY", "value", "SECTION");
```
3. Чтение числового значения ключа (<b>return: int</b>):
```csharp
iniFile.ReadInt("KEY", "SECTION");
```
4. Чтение логического значения ключа (<b>return: bool</b>):
```csharp
iniFile.ReadBool("KEY", "SECTION");
```
5. Получение всех ключей и их значений в определенной секции (<b>return: string[]</b>):
```csharp
iniFile.GetAllDataSection("SECTION");
```
6. Получение имен всех секций (<b>return: string[]</b>):
```csharp
iniFile.GetAllSections();
```
7. Удаление значения заданного ключа в определенной секции:
```csharp
iniFile.DeleteKey("KEY", "SECTION");
```
8. Удаление заданной секции:
```csharp
iniFile.DeleteSection("SECTION");
```
9. Чтение ключа по определенной секции и проверка наличия значения (<b>return: bool</b>):
```csharp
iniFile.KeyExists("KEY", "SECTION");
```
**P.S -> вы можете использовать разные имена ключей и секций.**
