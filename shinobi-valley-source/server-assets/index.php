<?php
	header("Access-Control-Allow-Origin: *");

	header("Access-Control-Allow-Methods: GET, POST");
	header("Access-Control-Allow-Headers: Content-Type, Access-Control-Allow-Headers, X-Requested-With");

	ini_set("precision", 16);
	
	$request_contents = file_get_contents('php://input');

	// Convert JSON to POST
	$_POST = json_decode($request_contents, true);

	include_once "autoloader.php";
	// include_once "crypto.php";

	$decryption_method = 'aes-256-cbc';

	// $plainkey = 'XAQSJ24dpj46J28w';
	$plainkey = 'le99AGxBQMfsO3gA';
	$hashedkey = substr(hash('sha256', $plainkey, false), 0, 16);

	// $postkey = 'Xh42g8bXv5HLsDUD';
	$postkey = '6QUITHAyoYm8RJsR';
	


	if (isset($_POST['key']))
	{
		$package = $_POST['key'];
		$iv = substr($package, strlen($package)-16,strlen($package));
		$message = substr($package, 0, strlen($package)-16);
		$decrypted = openssl_decrypt(base64_decode($message), $decryption_method, $hashedkey, OPENSSL_RAW_DATA, $iv);
	} else {
		die("Nope");
	}

	if ($decrypted === $postkey) {

		//if you want to add a new class to be recognized by the API, add it here
		$allowed = ['session', 'timeseries'];

		$local_uri = $_SERVER['REQUEST_URI'];

		list($path,) = explode('?', $local_uri);

		// Need to add initial ',' since path has a preceding '/' character, hence the first element after exploding the string is an empty string
		list(,$api_class, $api_method) = explode('/', $path);

		//make sure the class we're calling is allowed
		if(in_array($api_class, $allowed)){
			$class = new $api_class;

			//make sure we're calling a method that's allowed
			if($class->IsAllowed($api_method)){

				global $userip;
				$userip = "dfsf";

				echo call_user_func(array($class, $api_method));
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