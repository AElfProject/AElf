Install
=======

Before you get started with the tutorials, you need to install the
following tools and frameworks.

For most of these dependencies, we provide command line instructions for
macOS, Linux Ubuntu 18, and Windows. In case any problems occur or if
you have more complex needs, please leave a message on GitHub and we
will handle it ASAP.

macOS
-----

Configure Environment
~~~~~~~~~~~~~~~~~~~~~

You can install and set up the development environment on macOS
computers with either Intel or Apple M1 processors. This will take 10-20
minutes.

Before You Start
^^^^^^^^^^^^^^^^

Before you install and set up the development environment on a macOS
device, please make sure that your computer meets these basic
requirements:

-  Operating system version is 10.7 Lion or higher.

-  At least a 2Ghz processor, 3Ghz recommended.

-  At least 8 GB RAM, 16 GB recommended.

-  No less than 10 GB of available space.

-  Broadband internet connection.

**Support for Apple M1**

If you use a macOS computer with an Apple M1 chip, you need to install
Apple Rosetta. Open the Terminal on your computer and execute this
command,Please be patient while the command is executed.

.. code:: powershell

   /usr/sbin/softwareupdate --install-rosetta --agree-to-license

Install Homebrew
^^^^^^^^^^^^^^^^

In most cases, you should use Homebrew to install and manage packages on
macOS devices. If Homebrew is not installed on your local computer yet,
you should download and install it before you continue.

To install Homebrew:

1. Open Terminal.

2. Execute this command to install Homebrew:

   .. code:: bash

      /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"

3. Execute this command to check if Homebrew is installed:

   .. code:: bash

      brew --version

The following output suggests successful installation:

.. code:: bash

   Homebrew 3.3.1

   Homebrew/homebrew-core (git revision c6c488fbc0f; last commit 2021-10-30)

   Homebrew/homebrew-cask (git revision 66bab33b26; last commit 2021-10-30)

Environment Update
^^^^^^^^^^^^^^^^^^

Execute this command to update your environment:

.. code:: bash

   brew update

You will see output like this.

.. code:: bash

   You have xx outdated formula installed.
   You can upgrade it with brew upgrade
   or list it with brew outdated.

You can execute the following command to upgrade or skip to the
installation of Git.

.. code:: bash

   brew upgrade

Install Git
^^^^^^^^^^^

If you want to use our customized smart contract development environment
or to run a node, you need to clone aelf’s repo (download source code).
As aelf’s code is hosted on GitHub, you need to install **Git** first.

1. Execute this command in Terminal:

   .. code:: bash

      brew install git

2. Execute this command to check if Git is installed:

   .. code:: bash

      git --version

The following output suggests successful installation:

.. code:: bash

   git version xx.xx.xx

Install .NET SDK
^^^^^^^^^^^^^^^^

As aelf is mostly developed with .NET Core, you need to download and
install .NET Core SDK (Installers - x64 recommended for macOS devices
with Intel processors; Installers - Arm64 recommended for macOS devices
with M1 chips).

1. Download and install `.NET
   6.0 <https://dotnet.microsoft.com/en-us/download/dotnet/6.0>`__ which
   is currently used in aelf’s repo.

2. Please reopen Terminal after the installation is done.

3. Execute this command to check if .NET is installed:

   .. code:: bash

      dotnet --version

The following output suggests successful installation:

::

   6.0.403

Install protoBuf
^^^^^^^^^^^^^^^^

1. Execute this command to install protoBuf:

   .. code:: bash

      brew install protobuf

   If it shows error ``Permission denied @ apply2files``, then there is
   a permission issue. You can solve it using the following command and
   then redo the installation with the above command:

   .. code:: bash

      sudo chown -R $(whoami) $(brew --prefix)/*

2. Execute this command to check if protoBuf is installed:

   .. code:: bash

      protoc --version

The following output suggests successful installation:

.. code:: bash

   libprotoc 3.21.9

Install Redis
^^^^^^^^^^^^^

1. Execute this command to install Redis:

   .. code:: bash

      brew install redis

2. Execute this command to start a Redis instance and check if Redis is
   installed:

   .. code:: bash

      redis-server

The following output suggests Redis is installed and a Redis instance is
started:

.. figure:: mac_install_redis.png
   :alt: image


Install Nodejs
^^^^^^^^^^^^^^

1. Execute this command to install Nodejs:

   .. code:: bash

      brew install node

2. Execute this command to check if Nodejs is installed:

   .. code:: bash

      npm --version

The following output suggests successful installation:

::

   6.14.8

Linux
-----

.. _configure-environment-1:

Configure Environment
~~~~~~~~~~~~~~~~~~~~~

You can install and set up the development environment on computers
running 64-bit Linux. This will take 10-20 minutes.

.. _before-you-start-1:

Before You Start
^^^^^^^^^^^^^^^^

Before you install and set up the development environment on a Linux
device, please make sure that your computer meets these basic
requirements:

-  Ubuntu 18.

-  Broadband internet connection.

Update Environment
^^^^^^^^^^^^^^^^^^

Execute this command to update your environment, Please be patient while
the command is executed:

.. code:: bash

   sudo apt-get update

The following output suggests successful update:

.. code:: bash

   Fetched 25.0 MB in 3s (8,574 kB/s)
   Reading package lists... Done

.. _install-git-1:

Install Git
^^^^^^^^^^^

If you want to use our customized smart contract development environment
or to run a node, you need to clone aelf’s repo (download source code).
As aelf’s code is hosted on GitHub, you need to install **Git** first.

1. Open the terminal.

2. Execute this command to install Git:

   .. code:: bash

      sudo apt-get install git -y

3. Execute this command to check if Git is installed:

   .. code:: bash

      git --version

The following output suggests successful installation:

.. code:: bash

   git version 2.17.1

.. _install-.net-sdk-1:

Install .NET SDK
^^^^^^^^^^^^^^^^

As aelf is mostly developed with .NET Core, you need to download and
install .NET Core SDK.

1. Execute the following commands to install .NET 6.0.

   1. Execute this command to download .NET packages:

      .. code:: bash

         wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb

   2. Execute this command to unzip .NET packages:

      .. code:: bash

         sudo dpkg -i packages-microsoft-prod.deb

         rm packages-microsoft-prod.deb

   3. Execute this command to install .NET:

      .. code:: bash

         sudo apt-get update && \

         sudo apt-get install -y dotnet-sdk-6.0

2. Execute this command to check if .NET 6.0 is installed:

   .. code:: bash

      dotnet --version

The following output suggests successful installation:

::

   6.0.403

.. _install-protobuf-1:

Install protoBuf
^^^^^^^^^^^^^^^^

Before you start the installation, please check the directory you use
and execute the following commands to install.

1. Execute the following commands to install protoBuf.

   1. Execute this command to download protoBuf packages:

      .. code:: bash

         curl -OL https://github.com/google/protobuf/releases/download/v21.9/protoc-21.9-linux-x86_64.zip

   2. Execute this command to unzip protoBuf packages:

      ::

         unzip protoc-21.9-linux-x86_64.zip -d protoc3

   3. Execute these commands to install protoBuf:

      .. code:: bash

         sudo mv protoc3/bin/* /usr/local/bin/

         sudo mv protoc3/include/* /usr/local/include/

         sudo chown ${USER} /usr/local/bin/protoc

         sudo chown -R ${USER} /usr/local/include/google

      If it shows error ``Permission denied @ apply2files``, then there
      is a permission issue. You can solve it using the following
      command and then redo the installation with the above commands:

      .. code:: bash

         sudo chown -R $(whoami) $(brew --prefix)/*

2. Execute this command to check if protoBuf is installed:

   .. code:: bash

      protoc --version

The following output suggests successful installation:

::

   libprotoc 3.21.9

.. _install-redis-1:

Install Redis
^^^^^^^^^^^^^

1. Execute this command to install Redis:

   .. code:: bash

      sudo apt-get install redis -y

2. Execute this command to start a Redis instance and check if Redis is
   installed:

   ::

      redis-server

The following output suggests Redis is installed and a Redis instance is
started:

::

   Server initialized
   Ready to accept connections

You can open a new terminal and use redis-cli to start Redis command
line. The command below can be used to clear Redis cache (be careful to
use it):

::

   flushall

.. _install-nodejs-1:

Install Nodejs
^^^^^^^^^^^^^^

1. Execute these commands to install Nodejs:

   .. code:: bash

      curl -fsSL https://deb.nodesource.com/setup_14.x | sudo -E bash -

      sudo apt-get install -y nodejs

2. Execute this command to check if Nodejs is installed:

   .. code:: bash

      npm --version

The following output suggests successful installation:

::

   6.14.8

Windows
-------

.. _configure-environment-2:

Configure Environment
~~~~~~~~~~~~~~~~~~~~~

You can install and set up the development environment on computers
running Windows 10 or higher. This will take 10-20 minutes.

.. _before-you-start-2:

Before You Start
^^^^^^^^^^^^^^^^

Before you install and set up the development environment on a Windows
device, please make sure that your computer meets these basic
requirements:

-  Operating system version is Windows 10 or higher.

-  Broadband internet connection.

Install Chocolatey (Recommended)
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

**Chocolatey** is an open-source package manager for Windows software
that makes installation simpler, like Homebrew for Linux and macOS. If
you don’t want to install it, please use the provided download links for
each software to complete their installation.

1. Open **cmd** or **PowerShell** as administrator (Press Win + x).

2. Execute the following commands in order and enter y to install
   Chocolatey, Please be patient while the command is executed:

   .. code:: powershell

      Set-ExecutionPolicy AllSigned

      Set-ExecutionPolicy Bypass -Scope Process

      Set-ExecutionPolicy Bypass -Scope Process -Force; iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))

      Set-ExecutionPolicy RemoteSigned

3. Execute this command to check if Chocolatey is installed:

   .. code:: powershell

      choco

The following output suggests successful installation:

::

   Chocolatey vx.x.x

If it
shows\ ``The term 'choco' is not recognized as the name of a cmdlet, function, script file, or operable program``,
then there is a permission issue with PowerShell. To solve it:

-  **Right-click** the computer icon and select **Properties**.

-  Click **Advanced** in **System Properties** and select **Environment
   Variables** on the bottom right.

-  Check if the **ChocolateyInstall variable** is in **System
   variables**, and its default value is the Chocolatey installation
   path ``C:\Program Files\Chocolatey``. If you don’t find it, click New
   System Variable to manually add it.

.. _install-git-2:

Install Git
^^^^^^^^^^^

If you want to use our customized smart contract development environment
or to run a node, you need to clone aelf’s repo (download source code).
As aelf’s code is hosted on GitHub, you need to install **Git** first.

1. You can download Git through this link or execute this command in cmd
   or PowerShell:

   .. code:: powershell

      choco install git -y

2. Execute this command to check if Git is installed:

   .. code:: powershell

      git --version

The following output suggests successful installation:

.. code:: powershell

   git version xx.xx.xx

If it shows
``The term 'git' is not recognized as the name of a cmdlet, function, script file, or operable program``,
you can:

-  **Right-click** the computer icon and select **Properties**.
-  Click **Advanced** in **System Properties** and select **Environment
   Variables** on the bottom right.
-  Check if the Git variable is in **Path** in **System variables**, and
   its default value is the Git installation path
   ``C:\Program Files\git``. If you don’t find it, click **New System
   Variable** to manually add it.

.. _install-.net-sdk-2:

Install .NET SDK
^^^^^^^^^^^^^^^^

As aelf is mostly developed with .NET Core, you need to download and
install .NET Core SDK (Installers - x64 recommended for Windows
devices).

1. Download and install `.NET
   6.0 <https://dotnet.microsoft.com/en-us/download/dotnet/6.0>`__ which
   is currently used in aelf’s repo.

2. Please reopen cmd or PowerShell after the installation is done.

3. Execute this command to check if .NET is installed:

   .. code:: powershell

      dotnet --version

   The following output suggests successful installation:

   ::

      6.0.403

.. _install-protobuf-2:

Install protoBuf
^^^^^^^^^^^^^^^^

1. You can download protoBuf through this link or execute this command
   in cmd or PowerShell:

   .. code:: powershell

      choco install protoc --version=3.11.4 -y

      choco install unzip -y

2. Execute this command to check if protoBuf is installed:

   ::

      protoc --version

The following output suggests successful installation:

::

   libprotoc 3.21.9

.. _install-redis-2:

Install Redis
^^^^^^^^^^^^^

1. You can download Redis through MicroSoftArchive-Redis or execute this
   command in cmd or PowerShell:

   .. code:: powershell

      choco install redis-64 -y

2. Execute this command to start a Redis instance and check if Redis is
   installed:

   ::

      memurai

The following output suggests Redis is installed and a Redis instance is
started:

.. figure:: windows_install_redis.png
   :alt: image

.. _install-nodejs-2:

Install Nodejs
^^^^^^^^^^^^^^

1. You can download Nodejs through Node.js or execute this command in
   cmd or PowerShell:

   .. code:: powershell

      choco install nodejs -y

2. Execute this command to check if Nodejs is installed:

   .. code:: powershell

      npm --version

The following output suggests successful installation:

::

   6.14.8

If it shows The term ‘npm’ is not recognized as the name of a cmdlet,
function, script file, or operable program, you can:

-  **Right-click** the computer icon and select **Properties**.

-  Click **Advanced** in **System Properties** and select **Environment
   Variables** on the bottom right.

-  Check if the Nodejs variable is in **Path** in **System variables**,
   and its default value is the Nodejs installation path
   ``C:\Program Files\nodejs``. If you don’t find it, click **New System
   Variable** to manually add it.

Codespaces
----------

A codespace is an instant development environment that’s hosted in the
cloud. It provides users with general-purpose programming languages and
tooling through containers. You can install and set up the development
environment in Codespaces. This will take 10-20 minutes. Please be
patient while the command is executed.

Basic Environment Configurations
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

1. Visit `AElfProject / AElf <https://github.com/AElfProject/AElf>`__
   via a browser.

2. Click the green **Code** button on the top right.

   .. figure:: codespaces1.png
      :alt: image

3. Select ``Codespaces`` and click +.

   .. figure:: codespaces2.png
      :alt: image2


Then a new tab will be opened that shows the ``Codespaces`` interface.
After the page is loaded, you will see:

-  The left side displays all the content in this repo.

-  The upper right side is where you can write code or view text.

-  The lower right side is a terminal where you can build and run code
   (If the terminal doesn’t open by default, you can click the hamburger
   menu on the top left and select Terminal -> New Terminal, or press
   control + shift + \` on your keyboard).

Currently, ``Codespaces`` have completed the configuration for part of
the environments, yet there are some you need to manually configure.

At the time of writing, ``Codespaces`` have done the configuration for
git and nodejs. You can type the following commands to check their
versions:

.. code:: bash

   # git version 2.25.1
   git --version

   # 8.19.2
   npm --version

.. _update-environment-1:

Update Environment
^^^^^^^^^^^^^^^^^^

Execute this command to update your environment:

.. code:: bash

   sudo apt-get update

The following output suggests successful update:

.. code:: bash

   Fetched 25.0 MB in 3s (8,574 kB/s)
   Reading package lists... Done

.. _install-.net-sdk-3:

Install .NET SDK
^^^^^^^^^^^^^^^^

.NET SDK 7.0 is used in this repo. Hence, you need to reinstall v6.0
otherwise there will be building issues.

1. Execute this command to check if v7.0 is used:

   .. code:: bash

      # 7.0.100
      dotnet --version

   If there is v7.0, execute this command to delete it:

   .. code:: bash

      sudo rm -rf /home/codespace/.dotnet/*

2. Execute this command to reinstall v6.0:

   .. code:: bash

      wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb

      sudo dpkg -i packages-microsoft-prod.deb

      rm packages-microsoft-prod.deb

      sudo apt-get update && \

      sudo apt-get install -y dotnet-sdk-6.0

3. Restart bash after the installation and execute this command to check
   if v6.0 is installed:

   .. code:: bash

      # 6.0.403
      dotnet --version

The following output suggests successful installation:

.. code:: bash

   6.0.403

.. _install-protobuf-3:

Install protoBuf
^^^^^^^^^^^^^^^^

1. Execute this command to install protoBuf:

   .. code:: bash

      curl -OL https://github.com/google/protobuf/releases/download/v21.9/protoc-21.9-linux-x86_64.zip
      unzip protoc-21.9-linux-x86_64.zip -d protoc3

      sudo mv protoc3/bin/* /usr/local/bin/

      sudo mv protoc3/include/* /usr/local/include/

      sudo chown ${USER} /usr/local/bin/protoc

      sudo chown -R ${USER} /usr/local/include/google

2. Execute this command to check if protoBuf is installed:

   .. code:: bash

      protoc --version

The following output suggests successful installation:

.. code:: bash

   libprotoc 3.21.9

.. _install-redis-3:

Install Redis
^^^^^^^^^^^^^

1. Execute this command to install Redis:

   .. code:: bash

      sudo apt-get install redis -y

2. Execute this command to start a Redis instance and check if Redis is
   installed:

   .. code:: bash

      redis-server

The following output suggests Redis is installed and a Redis instance is
started:

.. code:: bash

   Server initialized
   Ready to accept connections

What’s Next
^^^^^^^^^^^

If you have already installed the tools and frameworks above, you can
skip this step. For info about contract deployment and nodes running,
please read the following:

`Smart contract
development <https://docs.aelf.io/en/latest/getting-started/smart-contract-development/index.html>`__

`Smart contract
deployment <https://docs.aelf.io/en/latest/getting-started/smart-contract-development/index.html>`__

`Node <../../getting-started/development-environment/node.html>`__
