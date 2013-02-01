PublicSwBPlugins
================

A place to file your pull requests for modules to go into swb

Each new addition is a new project, This is so I can merge it with the private code.

You have the following commands to interface with the system:

**void discord.core.Discord.SendChatMessage(ChatRoomID, String);**

` - Sends a chat message to a chatroom.`


**bool discord.core.Discord.IsActiveCRoon(ChatRoomID)**

` - Check if the chatID wants to have chat messages picked up (for example youtube resolving)`

**string[,] discord.core.Discord.DoQueryArray(Query,ExpectedCol, ExpectedRows)**


` - Gets a MySQL query in a 2D string, the array is laid out like string[Row,Col]`

**string discord.core.Discord.DoClassicQuery(Query)**

` - This will return a single string that will have each col sepearted by , and new rows \n`

**void discord.core.Discord.DoQueryNonRead(Query)**

` - This is used for INSERT or UPDATE querys`

**string discord.core.Discord.MySQLEscape**

` - Escapes a string for inserting into a database`
