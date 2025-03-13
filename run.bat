@echo off
dotnet run --debug --settings-diff auto > output.txt
type output.txt
