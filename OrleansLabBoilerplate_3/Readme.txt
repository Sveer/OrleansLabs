# ������������ ������ �� ������� Microsopft Orleans

��� ����� ��� ����������:
1. Visual Studio 2019 Preview 4 - ����������� ������� (https://docs.microsoft.com/en-us/visualstudio/releases/2019/release-notes-preview)
2. .NET Core 3.0 RC1 (https://dotnet.microsoft.com/download/dotnet-core/3.0)
3. ���������� � ������ ����� ��������� ��� Orleans: https://hz.pryaniky.com/azureday/snippets


## ��������� Boilerplate'a:
 - OneBoxDeployment.OrleansUtilities - ������ �� �������� Orleans - �������� ��������� ������� ��� ������ ������������ Silo �� json-����� ������������
 - Pryaniky.OrleansHost - ���������� ������ � �������� Silo
 - Pryaniky.Orleans.GrainInterfaces - ������ ������ ��� ����������� �������
 - Pryaniky.Orleans.Grains  - ������ ������ ��� ���������� �������

## � ������ ��������� �� ���� ���� ��������
1. �������� � ������ �������� ������ (User)
� Orleans ���� ��� �������� ��������:
Grain � ����������� ����� (��������� ��������� IGrain, IGrainWithXXXKey)
Silo � ����, ������������� ������ Grain

������  - ������� ������� Grain �������������� (User) � ��������� ��� � Silo
��� ����� � BoilerPlate ������� ����� ��������� ��� �������-����������:
- GrainInterfaces
- Grains

� GrainInterfaces ��������� ��� Nuget-������:
Orleans.Abstractions � Orleans.CodeGeneration

