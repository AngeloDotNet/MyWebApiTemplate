# MinimalApi.Template

This is a template for creating a .NET web API project.
It includes the necessary files and structure to get started quickly with building a minimal web API using the latest .NET SDK.

## 🏷️ Introduction

**MinimalApi.Template** is a project template designed to help developers quickly set up a .NET web minimal API project.
It provides a basic structure and configuration to get you started with building your API without having to worry about the initial setup.

## 🛠️ Installation

### Prerequisites

- .NET 10.0 SDK

### Setup

The package is available on [BaGet](http://nuget.aepserver.it/packages/MinimalApi.Template), to install it use the following command in your terminal:

```shell
dotnet new install MinimalApi.Template
```

> [!WARNING]
> If you see the message "Could not install MinimalApi.Template. The package does not exist."
<!--
> If you need to force the template installation, you can use the following command:

<img width="1189" height="338" alt="image" src="https://github.com/user-attachments/assets/e1810ba8-6e10-4c64-be62-56761d5521c2" />
<br />
-->

You will need to run the command with the --force option to install the template correctly, as shown in the line below.

```shell
dotnet new install MinimalApi.Template --force
```

> [!IMPORTANT]
> Since the template is published to Baget (and not to NuGet), you need to add a new dedicated NuGet source using the following command:

```shell
dotnet nuget add source http://nuget.aepserver.it/v3/index.json --allow-insecure-connections --name Baget
```

To delete the template, it using the following command in the terminal:

```shell
dotnet new uninstall MinimalApi.Template
```

> [!TIP]
> To update the template, you will need to uninstall the template and then reinstall it using the terminal commands above.

## 🚀 Getting Started

To create a new Web API project using this template, run the following command in your terminal:

```shell
dotnet new minimalapi -n <YourProjectName>
```

<!--
## 💡 Usage Examples
-->

## 📜 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

<!-->
## ⭐ Give a Star

Don't forget that if you find this project helpful, please give it a ⭐ on GitHub to show your support and help others discover it.

## 🤝 Contributing

Contributions are always welcome. Feel free to report issues and submit pull requests to the repository, following the steps below:

1. Fork the repository
2. Create a feature branch (starting from the develop branch)
3. Make your changes
4. Submit a pull requests (targeting develop)
-->