
# EmbodiedFlightSimVR
 Educational-cooperative VR flight simulator. UoA postgraduate dissertation.

### Usage notes
Tested with FlightGear 2020.3 and [Mirage 2000-5](https://wiki.flightgear.org/Dassault_Mirage_2000-5)
- Minimum 1 and maximum 2 users are required (in case of only 1 user, should be an Instructor)
- Normally both users should run the app on Quest 2 headsets, but one of them can run it in the Unity Editor instead
- Upon starting the app, each user must first choose a role (Instructor or Student):
  - On Quest 2 headset, use the menu buttons on your left to choose
  - On Unity Editor, press Tab to select/toggle between the roles
- Once both users have chosen their roles, run `CustomTools/connection_broker.py` to initialize the connection (don't open FlightGear beforehand, it will be opened automatically by the broker)
  - If one or both users can't connect successfully or if FlightGear doesn't start automatically, check the terminal log
  - You will need to update the script manually if FlightGear is not installed in default directory `C:/Program Files/FlightGear 2020.3/bin`
- Once FlightGear is started, choose Mirage 2000 > Configuration > Auto-Start to bring the aircraft to ready-to-fly state

### Controls
Quest 2:
- Grab and move throttle & stick to control aircraft
- RH controller thumbstick left/right: rudder left/right
- RH controller thumbstick down: brake

Unity Editor:
- WSAD: pitch/roll
- Up/down arrow keys: increase/decrease throttle
- Left/right arrow keys: rudder left/right
- G: gear up/down
- Tab: toggle Instructor/Student role (only before connecting to FlightGear)

### Developer notes
When running on Oculus (not in editor), Mapbox fails to load due to a `NullReferenceException` in `MapboxConfiguration.GetMapsSkuToken()` (file `Assets\Mapbox\Unity\MapboxAccess.cs:342`). A workaround is to modify it to directly return your Mapbox API token (either hardcoded or fetched from another source).
