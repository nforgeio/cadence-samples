## cadence-samples

This repository holds samples of workflows and activities written in .NET for the Uber Cadence platform.  These have been tested on Windows and Mac OS/X using Visual Studio.  We'll have you run these workflows against Cadence/Cassandra running locally as a Docker container.

### Requirements

You'll need a computer configured like:

* A 64-bit Windows computer running Windows 10 with Hyper-V or a Mac running a recent version of OS/X.
* 4+ CPU cores
* Visual Studio 2019
* 8GB+ RAM
* Docker Desktop

Note that you'll need to enable Hyper-V for Windows because Docker requires it.  The Docker installation link explains how to accomplish this.

### Configuring Windows

1. Download and install **Visual Studio 2019**.  The **Community Edition** works fine and is free for most users, but any edition will work:

   [https://visualstudio.microsoft.com/vs/](https://visualstudio.microsoft.com/vs/)

2. Install Docker Desktop (community) by following the instructions here:

   [https://docs.docker.com/docker-for-windows/install/](https://docs.docker.com/docker-for-windows/install/)

3. Make sure you start Docker after installing it.

4. Increase Docker CPUs to 4 and RAM allocation to 4GB.

   a. **Right-click the Docker icon** in your system tray and select **Settings**
   b. Click **Advanced** on the left and then drag the **CPU slider to 4** and the **Memory slider to 4096**
   c. Click **Apply**

5. Configure Docker networking: We've never had much with the default Docker DNS network settings.  We recommend configuring Docker to use Google Public DNS (8.8.8.8):

   a. **Right-click the Docker icon** in your system tray and select **Settings**
   b. Click **Network** on the left
   c. Select **Fixed** in the **DNS Server** section and enter **8.8.8.8** as the server address.
   d. Click **Apply**

6. Open a command window and pull the Docker container image we'll be using host the Cadence service.  This image bundles a recent version of Cadence along with Cassandra making it easy to quickly deploy Cadence locally for development and testing purposes.
   ```
   docker pull nkubeio/cadence-dev:latest
   ```
   The image is about 500MB so this may take some time to download.

### Remarks

You may be able to adjust the Docker RAM allocation lower.  We encountered stablity issues with the default 2GB allocation and doubled this to 4GB for our purposes with good results.  There needs to be enough RAM to run the Docker Linux distribution along with the Cadence and Cassandra services.

On Windows, Docker always consumes the configured RAM, even if it's not actually doing anything.  We develop on Windows workstations with 32-64GB of RAM so the loss of 4GB doesn't bother us, but this can impact lesser machines.  You may want to exit and disable Docker when your not using it.  Use the **Settings/General** to disable Docker startup.

The current Docker on Windows implementation is hack compared to how Docker works natively on Linux and OS/X.  **Windows Docker is somewhat unstable** and we find it can lose network connectivity every few days.  Image pulls and image builds will fail and executing containers will be unable to establish network connections.  You can mitigate this by **right-clicking** on the Docker icon in the system tray and selecting **Restart...*  Docker is working on new implementation that takes advantage of the Windows Subsystem for Linux (WSL) which will eventually address these disavantages.

### Configuring Macintosh

1. Download and install **Visual Studio 2019 for Mac**:

   [https://visualstudio.microsoft.com/vs/mac/](https://visualstudio.microsoft.com/vs/mac/)

2. Install **Docker for Mac**:

   https://docs.docker.com/docker-for-mac/install/

3. Open a terminal window and Open a command window and pull the Docker container image we'll be using host the Cadence service. This image bundles a recent version of Cadence along with Cassandra making it easy to quickly deploy Cadence locally for development and testing purposes.
   ```
   docker pull nkubeio/cadence-dev:latest
   ```
   The image is about 500MB so this may take some time to download.

## Cadence Docker image: nforgeio/cadence-dev

We've published the [nforgeio/cadence-dev](https://hub.docker.com/repository/docker/nkubeio/cadence-dev) Docker image to be an easy way to spin up a fully functional Cadence server including the Cadence UX portal and a dedicated Cassandra databaser server node.

This container is very easy to manually manage from the command line:
```
# This command starts a cadence container named: cadence
#
# The container will listen on all workstation network interfaces using the standard Cadence and Cassandra ports:
#
#       Cadence:   7933,7934,7935,7939
#       Cassandra: 9042

docker run --detach --name cadence nforgeio/cadence-dev

# This command stops and removes the Cadence container.  Note that you'll lose any data persisted to Cassandra:

docker rm --force --name cadence
```

Once Cadence has started, you can view its portal via: [http://localhost:7933](http://localhost:7933)

# CadenceFixture: Easy Cadence unit testing

For those of you using [Xunit](https://github.com/xunit/xunit) for unit testing, we make the [CadenceFixture](https://doc.neonkube.com/T_Neon_Xunit_Cadence_CadenceFixture.htm) test fixture available.  Add this to your unit tests to automatically spin up a local Cadence server instance while your tests are running.

This repository includes examples demonstrating how to do this.

