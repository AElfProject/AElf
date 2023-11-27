Quick Start
===========

This section will provide you with a quick walkthrough on how to use our aelf-developer-tools and 
contract templates to generate a contract project and perform unit testing.

1. Install templates
--------------------
Run the following command to install the HelloWorld contract template on your local environment.

::

    dotnet new install AElf.ContractTemplates

2. create and initialize project
--------------------------------
Next, after installation, create a folder named 'HelloWorld' using the following command.

::

    mkdir HelloWorld
    cd HelloWorld

Then, let's run the following command to initialize project.

::

    dotnet new aelf -n HelloWorld

After completing this step, you will be able to see the contract project structure. 
The **src** directory contains the contract source code, and the **test** directory contains the unit testing code.

3. build contract project
-------------------------
In this case, we won't modify any source code and test code. Let's run the build command directly.

::

    cd src
    dotnet build

After building, if you see the results and there are no errors, it means the build was successful.

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
In the next section, we will develop a Greeter contract based on the HelloWorld contract 
and provide a detailed introduction to contract development.