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
> Since the template is not published on Nuget but on Baget (nuget custom) it is necessary to add a new dedicated nuget source using the following command:

```shell
dotnet nuget add source http://nuget.aepserver.it/v3/index.json --allow-insecure-connections --name Baget
```

<!--
> [!TIP]
> If new versions of the template appear, you can update it using the following command in the terminal:

```shell
dotnet new --update MinimalApi.Template
```

> [!TIP]
> You can delete it using the following command in the terminal:

```shell
dotnet new --uninstall MinimalApi.Template
```
-->

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

## ⭐ Give a Star

Don't forget that if you find this project helpful, please give it a ⭐ on GitHub to show your support and help others discover it.

## 🤝 Contributing

Contributions are always welcome. Feel free to report issues and submit pull requests to the repository, following the steps below:

1. Fork the repository
2. Create a feature branch (starting from the develop branch)
3. Make your changes
4. Submit a pull requests (targeting develop)