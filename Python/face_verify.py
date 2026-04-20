import sys
import os
import base64
import json
import numpy as np
import cv2
import face_recognition

def decode_base64_image(base64_string):
    # Strip metadata if exists (e.g. data:image/png;base64,...)
    if ',' in base64_string:
        base64_string = base64_string.split(',')[1]
    
    # Fix padding if necessary
    padding = len(base64_string) % 4
    if padding != 0:
        base64_string += '=' * (4 - padding)
        
    img_data = base64.b64decode(base64_string)
    nparr = np.frombuffer(img_data, np.uint8)
    img = cv2.imdecode(nparr, cv2.IMREAD_COLOR)
    return img

def register_face(image_base64, save_path):
    try:
        img = decode_base64_image(image_base64)
        if img is None:
            return {"success": False, "message": "Invalid image data."}

        # Convert to RGB (face_recognition uses RGB)
        rgb_img = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)
        
        # Get face encodings
        encodings = face_recognition.face_encodings(rgb_img)
        
        if not encodings:
            return {"success": False, "message": "No face detected in the image."}
        
        # Save the first encoding
        np.save(save_path, encodings[0])
        return {"success": True, "message": "Face registered successfully."}
    except Exception as e:
        return {"success": False, "message": str(e)}

def verify_face(image_base64, face_data_dir):
    try:
        img = decode_base64_image(image_base64)
        if img is None:
            return {"success": False, "message": "Invalid image data."}

        rgb_img = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)
        unknown_encodings = face_recognition.face_encodings(rgb_img)

        if not unknown_encodings:
            return {"success": False, "message": "No face detected in the capture."}

        unknown_encoding = unknown_encodings[0]
        
        # Load all registered encodings
        known_encodings = []
        known_identifiers = []

        if not os.path.exists(face_data_dir):
            return {"success": False, "message": "Face database empty."}

        for filename in os.listdir(face_data_dir):
            if filename.endswith(".npy"):
                path = os.path.join(face_data_dir, filename)
                encoding = np.load(path)
                known_encodings.append(encoding)
                known_identifiers.append(os.path.splitext(filename)[0])

        if not known_encodings:
            return {"success": False, "message": "No registered users found."}

        # Compare face
        results = face_recognition.compare_faces(known_encodings, unknown_encoding, tolerance=0.6)
        face_distances = face_recognition.face_distance(known_encodings, unknown_encoding)
        
        best_match_index = np.argmin(face_distances)
        
        if results[best_match_index]:
            identifier = known_identifiers[best_match_index]
            return {
                "success": True, 
                "match": True, 
                "identifier": identifier,
                "confidence": float(1 - face_distances[best_match_index])
            }
        else:
            return {"success": True, "match": False, "message": "Face not recognized."}

    except Exception as e:
        return {"success": False, "message": str(e)}

if __name__ == "__main__":
    # Expecting: python face_verify.py <command> <arg>
    # command: 'register' or 'verify'
    # arg: save_path for register, face_data_dir for verify
    # image base64 provided via stdin
    
    if len(sys.argv) < 3:
        print(json.dumps({"success": False, "message": "Missing arguments."}))
        sys.exit(1)

    command = sys.argv[1]
    argument = sys.argv[2]
    
    # Read base64 from stdin
    input_data = sys.stdin.read().strip()
    
    if command == "register":
        result = register_face(input_data, argument)
    elif command == "verify":
        result = verify_face(input_data, argument)
    else:
        result = {"success": False, "message": f"Unknown command: {command}"}

    print(json.dumps(result))
