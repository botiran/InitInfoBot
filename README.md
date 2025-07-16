# InitInfo Bot

A focused C#/.NET Telegram bot for administrators. Its primary function is to provide essential information about supergroups and channels. When an admin adds the bot to a chat, it instantly sends a report with the chat ID, member count, and invite link back to them.

![.NET](https://img.shields.io/badge/.NET-9-blue.svg)
![License](https://img.shields.io/badge/License-MIT-green.svg)

---

## Introduction

During the development of various Telegram-related applications and bots, I frequently needed to retrieve the unique IDs of channels and supergroups. While many public bots offer this functionality, I wanted a secure and trustworthy solution that I could confidently grant administrative privileges to. This project was born out of the need for a simple, personal, and reliable tool to get chat information without compromising on security.

This bot is designed to be self-hosted, ensuring that your data and the bot's access tokens remain private.

A public instance of this bot is also available on Telegram at [@InitInfoBot](https://t.me/InitInfoBot). Feel free to use it directly, or host your own version for maximum privacy and control.

## Features

-   **Instant Chat Reports**: Get chat ID, member count, and an invite link as soon as the bot is added to a new group or channel.
-   **On-Demand Updates**: Fetch fresh reports for all your managed chats with a single command.
-   **User-Friendly Commands**: A simple set of commands to manage the bot and retrieve information.
-   **Secure & Self-Hosted**: You control the environment, the code, and your bot's token.
-   **Configurable**: Easily change the bot's name, token, and database settings via a JSON configuration file.
-   **Simple Architecture**: Built with modern .NET, Dependency Injection, and a separated data layer for easy maintenance and extension.

## Getting Started

Follow these instructions to get your own instance of the bot up and running.

### Prerequisites

-   [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later.
-   A Telegram Bot Token from [@BotFather](https://t.me/BotFather).

### 1. Installation

Clone the repository to your local machine:

```bash
git clone [https://github.com/botiran/InitInfoBot.git](https://github.com/botiran/InitInfoBot.git)
cd InitInfoBot
```

### 2. Configuration

Before running the bot, you need to configure it. Rename the `appsettings.example.json` file to `appsettings.json` and fill in the required values.

```json
{
  "BotConfiguration": {
    "BotToken": "YOUR_TELEGRAM_BOT_TOKEN",
    "ConnectionString": "Data Source=InitInfoBot.db",
    "BotName": "InitInfo Bot",
    "TelegramBotApiServer": ""
  }
}
```

**Configuration Fields:**

-   `BotToken`: **(Required)** Your unique bot token obtained from BotFather.
-   `ConnectionString`: The connection string for the SQLite database. The default setting is usually sufficient.
-   `BotName`: The name that the bot will use in its welcome and help messages.
-   `TelegramBotApiServer`: (Optional) If you are using a private Telegram Bot API server, enter its URL here (e.g., `http://localhost:8081`). Otherwise, leave it empty.

### 3. Running the Bot

Once the configuration is complete, you can run the bot from the project's root directory using the following command:

```bash
dotnet run
```

The bot will start, connect to the Telegram API, and will be ready to receive commands and updates.

## Usage Guide

Using the bot is straightforward.

### Initial Setup

1.  Start a private chat with your bot on Telegram and send the `/start` command. This will register you as a user.
2.  Add the bot to any supergroup or channel where you have administrative rights. You must at least grant the bot permission to view messages. For the bot to be able to generate invite links, it needs the "Invite Users via Link" permission.

As soon as the bot is added, it will send you a private message containing the chat's information.

### Available Commands

All commands should be sent in a private chat with the bot.

-   `/start`
    Displays the welcome message and a summary of commands.

-   `/help`
    Shows a detailed list of all available commands and their descriptions.

-   `/getme`
    Provides your Telegram user information, including your ID, first name, last name, and username.

-   `/updateall`
    Fetches a fresh, up-to-date report for every chat you have added the bot to. This is useful for getting the latest member counts.

-   `/removeall`
    A cleanup command. The bot will attempt to leave every group and channel you have added it to. The corresponding data will also be removed from its database.

## Technology Stack

-   **Framework**: .NET 9
-   **Telegram API Wrapper**: [Telegram.Bot](https://github.com/TelegramBots/Telegram.Bot)
-   **Database**: SQLite
-   **Data Access**: Dapper
-   **Logging**: Serilog

## License

This project is licensed under the MIT License. See the `LICENSE` file for details.
