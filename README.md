# xmpponent
Component framework for XMPP servers.

Take a look at the EchoBots class for a general idea of how it works.

To Use With Prosody:

First, create a subdomain for your component, such as component.example.com.
In your configuration file, add something like:

```
Component "component.example.com"
	component_secret = "my secret pass phrase"
```
