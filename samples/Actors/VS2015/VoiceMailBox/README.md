# VoiceMailBox

The VoiceMailBox sample uses the Reliable Actors framework to implement a simple voicemail system where each voicemail box is represented as an actor.

## Solution components

The VoiceMailBox solution contains the following projects:

- **VoiceMailBoxApplication**: An application project that links together the constituent services.
- **VoiceMailBox**: An Actor project containing the VoiceMailBoxActor type and the VoiceMailBox state type.
- **VoiceMailBox.Interfaces**: An interface project defining the contract for interacting with the VoiceMailBoxActor.
- **VoiceMailBoxWebService**: A stateless service project providing the web UI for the application.

## Running the sample on a local cluster

1. Load the VoiceMailBox solution in Visual Studio.
2. Hit F5 to start debugging.
3. Launch the browser to http://localhost:8081/voicemailbox
