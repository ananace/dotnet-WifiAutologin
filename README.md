Wifi Autologin
==============

A C# appliction for automatically navigating captive portals.

Configuration
-------------

A Linux-oriented example configuration;
```yaml
---
# Global configuration values are set here, they'll be used as fallbacks in case a specific network lacks them
_global:
  hooks:
    pre-login:
      - 'notify-send -i network-wireless-hotspot -u low -a wifi-autologin "WiFi Autologin" "Attempting to log into ${NETWORK}..."'

    login:
      - 'notify-send -i network-wireless-hotspot -u low -a wifi-autologin "WiFi Autologin" "Automatically logged into $NETWORK"'

    error:
      - 'notify-send -i network-wireless-hotspot -u low -a wifi-autologin "WiFi Autologin" "Failed to log into $NETWORK, $ERROR"'

#     - hook: 'nmcli conn up id "My VPN"'
#       unless: 'nmcli conn show --active | grep vpn'
#   data:
#     - 'echo "[$(date)] Retrieved data for $NETWORK" >> /tmp/data.log'
#
#     # Hooks can use if/unless to decide whether to run or not, both hook as well as if/unless are shell executed
#     # Break can be set to mark that no other hooks should run if this one succeeds
#     - hook: 'notify-send -i network-wireless-hotspot -u low -a wifi-autologin "Unlimited data available"'
#       if: 'test $DATA_INFINITE -eq 1'
#       break: true
#
#     - hook: 'notify-send -i network-wireless-hotspot -u low -a wifi-autologin "WiFi Autologin" "Available data: $DATA_AVAILABLE MB"'
#       if: 'test -n "$DATA_AVAILABLE"'
#       unless: 'test -n "$DATA_USED -a -n "$DATA_TOTAL"'
#
#     - hook: 'notify-send -i network-wireless-hotspot -u low -a wifi-autologin "WiFi Autologin" "Used data: $DATA_USED / $DATA_TOTAL MB"'
#       if: 'test -n "$DATA_USED" -a -n "$DATA_TOTAL"'

# Minimal example, an action will default to clicking the element if nothing else is specified
"Some Hotel Network":
  login:
    # Tick Terms of Service checkbox
    - '#tosAccept'
    # Click "Login" button
    - '#login'

# SSID-specific configuration
CaptiveNetwork:
  # Automatic detection will be attempted if not specified
  url: 'http://some-login-page.localdomain'

  # Steps necessary to finish a login, each step has a 5s timeout by default apart from script/sleep steps
  login:
    # Wait for the specified element to exist, then scroll it into view
    - acquire: '.login-box'
    # Enter the given value into the element matching the selector
    - element: '#emailBox'
      input: 'email@example.com'
    # Run JavaScript
    - script: '$("form#submit-form").submit()'
    # Wait half a second
    - sleep: 0.5
    # Click the element matching the selector
    - '#actionContinue'

  # Steps necessary to discover the available data amount on the network
  # Specified regexes will be matched against the element text, and are expected to capture at least one of avail_mb or used_mb and total_mb
  data:
    - '#networkInfo': '(?<avail_mb>\S+)MB remaining'

  # Network-specific hooks
  hooks:
    login:
      - nmcli c up id "VPN Network"
```
