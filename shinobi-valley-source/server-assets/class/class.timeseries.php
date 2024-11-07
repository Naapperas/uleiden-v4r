<?php
	class Timeseries extends Database{
		public function __construct(){
			parent::__construct();

			$this->allowed = ["post"];
		}

		public function Post(){
			// Check if all set

			// Check if id matches username
			$user_id = $_POST['id'];
			$user_name = $_POST['user'];

			$getname = $this->query("SELECT user FROM userdata WHERE id=?", array($user_id));


			if ($user_name != $getname[0]['user']) {
				return 'SERVER: ERROR -- Username Mismatch';
			}

			$data = $_POST['postdata'];

			// begin the sql statement
			$sql = "INSERT INTO timeseries (userdata_id, timestamp, logtype, logline ) VALUES ";

			$i = 0;
			foreach ($data as $series)
			{
				$i += 1;

				$sql .= "(";
				$sql .= "'"	. $series['userdata_id']. "',";
				$sql .= "FROM_UNIXTIME('"	. $series['timestamp']	. "'),";
				$sql .= "'"	. $series['logtype']	. "',";
				$sql .= "'"	. $series['logline'] 	. "'";
				
				$sql .= ")";

				if ($i < count($data))
				{
					$sql .= ",";
				}
				
			}

			$this->query($sql);

			return "SERVER: Timeseries Posted";

			// return $data[0]['logtype'];

			// $persp = $_POST['persp'];

			// if(isset($_POST["question"]) && isset($_POST["student"]) && isset($_POST['subtopic'])){
			// 	$this->query("INSERT INTO question (question, state, student_id, subtopic_id, timestamp) VALUES (?, ?, ?, ?, ?)", array($_POST['question'], "NONE", $_POST["student"], $_POST['subtopic'], $this->microtime_float()));
			// }
			// else{
			// 	echo "-3";
			// }
			// $user_ip = $_SERVER['REMOTE_ADDR'];
			// $ipnum = explode('.', $user_ip);
			// $hex_ip = sprintf('%02x%02x%02x%02x', $ipnum[0], $ipnum[1], $ipnum[2], $ipnum[3]);
			// $hex_time = dechex(time());
			// $username = 'P' . $hex_time . 'X' . $hex_ip;

			// $persp = $_POST['persp'];

			// $id = $this->query("INSERT INTO userdata (user, ipaddr, starttime, endtime, perspective) 
			// VALUES (?, ?, FROM_UNIXTIME(?), NULL, ?)", array( $username, $user_ip, time(), $persp), true);

			// return json_encode(array('id'=>$id,'user'=>$username));

			// $this->query("UPDATE game SET state=?", array($_POST['state']));


			// return $_POST['state'];
		}


		

	}
?>