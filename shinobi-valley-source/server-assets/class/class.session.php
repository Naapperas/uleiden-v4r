<?php
	class Session extends Database{
		public function __construct(){
			parent::__construct();

			$this->allowed = ["postnew", "postend"];
		}

		public function PostNew() {
			$user_ip = $_SERVER['REMOTE_ADDR'];
			$ipnum = explode('.', $user_ip);
			$hex_ip = sprintf('%02x%02x%02x%02x', $ipnum[0], $ipnum[1], $ipnum[2], $ipnum[3]);
			$username = $hex_ip . time();
			$username = strtoupper(dechex(crc32($username)));

			$unityparams = $_POST['parameters'];
			$timestamp = $_POST['timestamp'];

			// Condition chances -- random inclusive
			$style = (rand(1, 100) > 55 ? "NINJA" : "SPACE"); 	// right value equals chances for SPACE
			$patterns = rand(1, 100) > 25;						// right value equals chances for NO patterns
			$direction = (rand(1, 100) > 55 ? "A2B" : "B2A"); 	// right value equals chances for B2A
			$context = rand(1, 100) > 55;						// right value equals chances for NO context

			$log_params = $style . "_PAT:" .  var_export($patterns, true) . "_" . $direction . "_TXT:" . var_export($context, true) . "_" . $unityparams;

			$id = $this->query("INSERT INTO userdata (user, ipaddr, starttime, endtime, params) 
				VALUES (?, ?, datetime(?, 'unixepoch'), NULL, ?)", array( $username, $user_ip, $timestamp, $log_params), true);

			return json_encode(array(
				'id'		=>	$id,
				'user'		=>	$username,
				'style'		=>	$style,
				'patterns'	=>	$patterns,
				'direction'	=>	$direction,
				'context'	=>	$context
			));
		}

		public function PostEnd(){
			// Check if id matches username
			$user_id = $_POST['id'];
			$user_name = $_POST['user'];
			$timestamp = $_POST['timestamp'];

			$getname = $this->query("SELECT user FROM userdata WHERE id=?", array($user_id));

			if ($user_name != $getname[0]['user']) {
				return 'SERVER: ERROR -- Username Mismatch';
			}

			$sql = $this->query("UPDATE userdata SET endtime=datetime(?, 'unixepoch') WHERE id=?", array($timestamp, $user_id));
			
			return 'SERVER: Session End Posted';
		}





		

	}
?>