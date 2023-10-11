Quick Start
===========

This section will provide you with a quick walk through on how to use our aelf-tools
and contract templates to generate a contract project, and perform unit testing.

1. Install templates
--------------------
Run the following command to install HelloWorld contract template on your local environment.

::

    dotnet new install AElf.ContractTemplates

2. create and initialize project
--------------------------------
Next, we can create a folder named HelloWorld after installation. Using the following command.

::

    mkdir HelloWorld
    cd HelloWorld

Then, let's run the following command to initialize project.

::

    dotnet new aelf -n HelloWorld

After this, you can see the contract project structure. The source directory contains the contract source code,
and the test directory contains the unit testing code.

3. build contract project
-------------------------
In this case, we won't modify any source code and test code. So let's run the build command directly.

::

    cd src
    dotnet build

After building, if you see the results, and results have no errors. It means build succeed.

4. test contract project
------------------------
Now, we go to test directory, and build test project with the following command.

::

    cd ../test
    dotnet build

If the results have no errors. We can run the following command to test it out.

::

    dotnet test

So far, we have quickly walked through the development process of an aelf smart contract.
Next, in the next section, we will develop a Greeter contract base on HelloWorld contract
and provide a detailed introduction of contract development.