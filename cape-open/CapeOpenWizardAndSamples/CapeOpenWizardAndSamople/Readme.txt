capeopentoolkit.net UnitWizard, RegUnitAsm
Copyright (c) 2005-2008, Mose Srl (http://www.mose.units.it/)
-------------------------------------------------------------

The package contains two directories:
1)Wizard: the application wizard to make Cape Open Unit (version 1.0)
2)Sample: A Unit sample developed with the wizard tools

WIZARD
------
To run the wizard, just double click on UnitWizard.exe
The steps to do are few and simple:
1)Choose to "Create New Unit" and leave checked the mark "Generate Compatibility Types".
NEXT
2)Choose:
-a Name for the Unit (it will be the same for the class) (without blank spaces) 
-a Description
-a Project Name: this will be the name of the Visual Studio 2005 project and of the Assembly
-the other informations
NEXT
3)Leave Data Types Unchanged. They are Integer and Double Specification according to Cape Open Standard
NEXT
4)In each line you should define a parameter for the unit. Choose:
-A name
-A description
-A default value (if so, check also the mark "Def")
-A mode
-A type
NEXT
5)In each line you should define a port for the unit. Choose:
-A name
-A description
-A direction 
NEXT
6)Choose a destination path for the project and a language (C# or VB .NET). Leave the other information unchanged.
NEXT
7)Choose if you want to save.
FINISH

After this procedure is completed, you find the project in the chosen destination path.
Open the project and build it. It should do it without error.
After the project is built, in the folder Bin there will be the assembly of your unit, an application console named "RegUnitAsm", and a batch file named RegUnit. If you double click on the batch file, it will register your unit.

So all you have to do is write "OnCalculate" method (it is Calculate Method of the Cape Open ICapeUnit) and eventually the method "OnEdit". If you don't want to implement "OnEdit", put in it an instruction to raise an exception to delegate the simulator to manage the parameters.
