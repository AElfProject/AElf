## DevOps

## Open source development

We want to stay as open as possible during AElf's development. For this we follow a certain amount rules and guidelines to try and keep the project as accessible as possible. Our project is open source and we publish our code as well as current issues online. It is our responsibility to make it as transparent as possible.

AElf is a collaborative project and welcomes outside opinion and requests/discussion for modifications of the code, but since we work in an open environment all collaborator need to respect a certain standard. We clarify this in the following standard:

  - [collaborative standard](https://github.com/AElfProject/AElf/blob/dev/CODE_OF_CONDUCT.md)

We encourage collaborators that want to participate to first read the white paper and the documentations to understand the ideas surrounding AElf. Also a look at our code and architecture and the way current functionality has been implemented. After this if any questions remain, you can open an issues on GitHub stating as clearly as possible what you need to clarify.

Finally, any collaborator wanting to participate in the development should open a pull request following our rules. It will be formally reviewed and discussed through GitHub and if validated by core members of AElf, can be merged.

## Deployment

For versioning we use the semver versioning system: https://semver.org

Daily build

Integrated with github we have cron job that will publish the latest version of devs myget packets.

    - MyGet: https://www.myget.org/gallery/aelf-project-dev

Release branch

    - Nuget: https://www.nuget.org/profiles/AElf

## Testing

Testing is one of the most important aspects of software development. Non tested software is difficult to improve. There are two main types of testing that we perform: unit testing and performance testing. The unit testing covers functionality and protocol, which is an essential part of a blockchain system. The performance tests are also very important to show that modifications have not impacted the speed at which our nodes process incoming transactions and blocks.

### Unit testing

To ensure the quality of our system and avoid regression, as well as permit safe modifications, we try to cover as much of our functionality as possible through unit tests. We mostly use the xUnit framework and follow generally accepted best practices when testing. Our workflow stipulates that for any new functionality, we cover it with tests and make sure other unit tests.

### Perf testing

The performance testing is crucial to AElf since a strong point of our system is speed.

## Monitoring
  - Server monitoring: Zabbix monitors instances of aelf metrics like cpu, db...
  - Chain monitoring: project on github with Grafana dashboard from Influxdb
  - Akka monitoring: monitor actors.




