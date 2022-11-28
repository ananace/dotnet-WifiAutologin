Wifi Autologin
==============

A C# appliction for automatically navigating captive portals.

Configuration
-------------

A Linux-oriented example configuration;
```yaml
---
# Global configuration is set here
_global:
  # Default is to test all possible Selenium drivers, then using the one that works.
  # Selecting a driver in advance can speed up non-daemon action.
  driver: auto
  # driver: firefox
  # driver: chromium
  # driver: edge
  # driver: chrome

  # The URL to use for testing basic network connectivity with.
  # Tests will check if the resulting HTTP code is in the range 200-299.
  # test-url: http://detectportal.firefox.com/canonical.html

  # Hooks are shell commands that are executed as part of the navigation
  #   pre-login hooks run before the web driver launches
  #   login hooks run after the web driver has finished and network access has been verified
  #   error hooks run after the web driver has failed, either due to errors or due to failing the network access check
  #   data hooks run after login on a network with data actions
  # All hooks have access to the environment variable NETWORK which contains the name of the network being navigated
  # The error hooks additionally have access to the variable ERROR which includes an - often multi-line - error message
  # The data hooks can include the variables DATA_INFINTE, DATA_AVAILABLE, DATA_TOTAL, and DATA_USED - based on what the hooks find
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
#
#     - 'echo "[$(date)] Retrieved data for $NETWORK" >> /tmp/data.log'
#
#     # Hooks can use if/unless to decide whether to run or not
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

# SSID-specific configuration
CaptiveNetwork:
  # Will be detected automatically if not specified
  url: 'http://some-login-page.localdomain'
  # Will use _global or fallback if not specified
  test-url: 'http://internet-page.example.localdomain'

  # Steps necessary to finish a login, will automatically wait for elements to appear
  # Timeout in seconds can be specified with the value `timeout` - default is 5s
  login:
    # Enter the given value into the element matching the selector
    - element: '#emailBox'
      input: 'email@example.com'
    # action: input
    # Run JavaScript
    - script: '$("form#submit-form").submit()'
    # action: script
    # Wait half a second
    - sleep: 0.5
    # action: sleep
    # Click the element matching the selector
    - '#actionContinue'
    # action: click
    # Wait for DOM to settle
    - action: settle

  # Steps necessary to discover the available data amount on the network
  # Specified regexes will be matched against the element text, and are expected to capture at least one of avail_mb or used_mb and total_mb
  data:
    - '#networkInfo': '(?<avail_mb>\S+)MB remaining'

  # Network-specific hooks
  hooks:
    login:
      - nmcli c up id "VPN Network"

# Minimal example
"Some Hotel Network":
  login:
    # Tick Terms of Service checkbox
    - '#tosAccept'
    # Submit the "Login" form
    - action: submit
      element: '#login'
```
