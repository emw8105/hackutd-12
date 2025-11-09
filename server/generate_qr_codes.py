"""
QR Code Generator for Server Identification

This script generates QR codes for each server in the server_locations.json file.
Technicians can scan these QR codes with the AR app to fetch server information.

Usage:
    python generate_qr_codes.py

Output:
    Creates QR code images in the 'qr_codes/' directory
"""

import json
import qrcode
from pathlib import Path

def load_servers(json_file="server_locations.json"):
    """Load server data from JSON file"""
    with open(json_file, 'r') as f:
        data = json.load(f)
    # Handle both list and dict format
    if isinstance(data, list):
        return data
    return data.get('servers', [])

def generate_qr_code(data, filename, output_dir="qr_codes"):
    """
    Generate a QR code image
    
    Args:
        data: String data to encode
        filename: Output filename (without extension)
        output_dir: Directory to save QR codes
    """
    # Create output directory if it doesn't exist
    output_path = Path(output_dir)
    output_path.mkdir(exist_ok=True)
    
    # Generate QR code
    qr = qrcode.QRCode(
        version=1,  # Controls size (1 is smallest)
        error_correction=qrcode.constants.ERROR_CORRECT_H,  # High error correction
        box_size=10,
        border=4,
    )
    
    qr.add_data(data)
    qr.make(fit=True)
    
    # Create image
    img = qr.make_image(fill_color="black", back_color="white")
    
    # Save image
    output_file = output_path / f"{filename}.png"
    img.save(output_file)
    
    print(f"✓ Generated QR code: {output_file}")
    return output_file

def generate_server_qr_codes():
    """Generate QR codes for all servers"""
    try:
        servers = load_servers()
        
        if not servers:
            print("No servers found in server_locations.json")
            return
        
        print(f"Generating QR codes for {len(servers)} server(s)...\n")
        
        for server in servers:
            server_id = server['id']
            server_name = server.get('name', server_id)
            
            # Option 1: Simple server ID (recommended)
            qr_data = server_id
            
            # Option 2: JSON format with additional metadata (uncomment to use)
            # qr_data = json.dumps({
            #     "type": "server",
            #     "id": server_id,
            #     "name": server_name
            # })
            
            # Generate QR code
            filename = f"server_{server_id.replace('-', '_')}"
            generate_qr_code(qr_data, filename)
            
            print(f"  Server: {server_name}")
            print(f"  ID: {server_id}")
            print(f"  QR Data: {qr_data}\n")
        
        print(f"✓ All QR codes generated successfully!")
        print(f"  Location: qr_codes/")
        print(f"\n📝 Instructions:")
        print(f"  1. Print these QR codes")
        print(f"  2. Attach them to the physical server racks")
        print(f"  3. Scan with the AR app using peace sign gesture")
        
    except FileNotFoundError:
        print("❌ Error: server_locations.json not found!")
        print("   Make sure this script is run from the server directory")
    except Exception as e:
        print(f"❌ Error generating QR codes: {e}")

if __name__ == "__main__":
    generate_server_qr_codes()
