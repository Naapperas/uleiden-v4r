<?php
	header("Access-Control-Allow-Origin: *");

	header("Access-Control-Allow-Methods: GET, POST");
	header("Access-Control-Allow-Headers: Content-Type, Access-Control-Allow-Headers, X-Requested-With");

	ini_set("precision", 16);

	// ini_set('display_errors', 1);
	// ini_set('display_startup_errors', 1);
	// error_reporting(E_ALL);

	// //Make sure that it is a POST request.
	// if(strcasecmp($_SERVER['REQUEST_METHOD'], 'POST') != 0){
	// 	throw new Exception('Request method must be POST!');
	// }
	
	// //Make sure that the content type of the POST request has been set to application/json
	// $contentType = isset($_SERVER["CONTENT_TYPE"]) ? trim($_SERVER["CONTENT_TYPE"]) : '';
	// if(strcasecmp($contentType, 'application/json') != 0){
	// 	throw new Exception('Content type must be: application/json');
	// }
	
	// //Receive the RAW post data.
	// $rawcontent = trim(file_get_contents("php://input"));
	
	// //Attempt to decode the incoming RAW post data from JSON.
	// $jsondecoded = json_decode($rawcontent, true);
	
	// //If json_decode failed, the JSON is invalid.
	// if(!is_array($jsondecoded)){
	// 	throw new Exception('Received content contained invalid JSON!');
	// }
	
	// Convert JSON to POST
	$_POST = json_decode(file_get_contents('php://input'), true);

	include_once "autoloader.php";
	// include_once "crypto.php";

	$method = 'aes-256-cbc';

	$plainkey = 'XAQSJ24dpj46J28w';
	$hashedkey = substr(hash('sha256', $plainkey, false), 0, 16);

	$postkey = 'Xh42g8bXv5HLsDUD';
	


	if (isset($_POST['key']))
	{
		$package = $_POST['key'];
		$iv = substr($package, strlen($package)-16,strlen($package));
		$message = substr($package, 0, strlen($package)-16);
		$decrypted = openssl_decrypt(base64_decode($message), $method, $hashedkey, OPENSSL_RAW_DATA, $iv);
	} else {
		die("Nope");
	}

	if ($decrypted === $postkey) {

		//if you want to add a new class to be recognized by the API, add it here
		$allowed = ['session', 'timeseries'];

		//make sure the class we're calling is allowed
		if(in_array($_GET['class'], $allowed)){
			$class = new $_GET['class'];

			//make sure we're calling a method that's allowed
			if($class->IsAllowed($_GET['method'])){

				global $userip;
				$userip = "dfsf";

				echo call_user_func(array($class, $_GET['method']));
			}
			else{
				echo "-2";
			}
		}
		else{
			echo "-1";
		}
	} else {
		echo "-4";
	}

	
	/*
		ERROR CODES:
		-1 : invalid class
		-2 : invalid method
		-3 : incorrect parameters

	*/
?>