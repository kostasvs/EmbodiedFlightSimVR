import socket
import json
import subprocess
import time

def main():
    # receiving port of broker
    receiving_port = 5502
    # receiving port of the project app
    target_port = 5501
    # content of initial handshake message to the project app
    message_to_send = "connection_broker"
    # content of expected replies to get from project app
    expected_replies = ["master", "slave"]
    # command to run when ready to start FlightGear
    flightgear_cmd = ["C:/Program Files/FlightGear 2020.3/bin/fgfs.exe",
                      "--aircraft=m2000-5",
                      "--timeofday=morning",
                      "--generic=socket,in,25,,5500,udp,ventouras"]
    # Argument to append to the above flightgear_cmd before running.
    # Will be appended once for each app that responded,
    # replacing {target_ip} with the app address
    # and {target_port} with the target_port above (NOT the port from which the app replied)
    outbound_arg = "--generic=socket,out,25,{target_ip},{target_port},udp,ventouras"
    # working directory in which flightgear_cmd should be run
    flightgear_cmd_cwd = "C:/Program Files/FlightGear 2020.3/bin"
    # if not empty, specifies FlightGear's input address:port
    flightgear_address_port = ""

    local_ip = socket.gethostbyname(socket.gethostname())  # Get the local IP address
    base_ip = ".".join(local_ip.split(".")[:-1])  # Extract the base IP

    # Create a UDP socket
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.bind(("", receiving_port))
    sock.settimeout(5)  # Set a timeout for receiving

    # send message to all IPs in local network
    for i in range(1, 255):
        ip = f"{base_ip}.{i}"
        sock.sendto(message_to_send.encode(), (ip, target_port))

    missing_replies = expected_replies.copy()
    addresses = {}
    addresses_encoded = {}
    while len(missing_replies) > 0:
        try:
            # Wait for any response
            data, addr = sock.recvfrom(1024)
            msg = data.decode()
            print(f"Received from {addr[0]}:{addr[1]} - {msg}")
            if msg in missing_replies:
                missing_replies.remove(msg)
                addresses[msg] = addr
                addresses_encoded[msg] = f"{addr[0]}:{addr[1]}"

        except socket.timeout:
            print("Socket timeout")
            break
        except ConnectionResetError:
            # Connection reset by remote host, reopen socket
            sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
            sock.bind(("", receiving_port))
            sock.settimeout(5)  # Set a timeout for receiving

    for i in missing_replies:
        print(f"Did not receive {i} response")

    # send type/address dictionary to all relevant addresses
    addresses_encoded['flightGear'] = flightgear_address_port
    addr_json = 'clients: ' + json.dumps(addresses_encoded)
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    for i in addresses:
        addr = addresses[i]
        sock.sendto(addr_json.encode(), (addr[0], target_port))
        # also add as argument to flightgear command,
        # so flightgear will send simulation data to that client
        flightgear_cmd.append(outbound_arg
                              .replace("{target_ip}", addr[0])
                              .replace("{target_port}", str(target_port)))

    sock.close()

    if len(missing_replies) == len(expected_replies):
        print(f"No responses received, aborting FlightGear launch")
        time.sleep(5)
    else:
        # run flightgear
        print(flightgear_cmd)
        subprocess.Popen(flightgear_cmd, cwd=flightgear_cmd_cwd)
        time.sleep(3)

if __name__ == "__main__":
    main()
