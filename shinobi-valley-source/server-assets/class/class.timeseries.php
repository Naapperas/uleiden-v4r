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

			// Safe destructuring since we are only expecting a single user
			$results = $this->query("SELECT user FROM userdata WHERE id=?", array($user_id));
			
			if (count($results) != 1) {
				return 'SERVER: ERROR -- No User Found';
			}

			$record = $results[0];

			if ($user_name != $record['user']) {
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
				$sql .= "datetime('"	. $series['timestamp']	. "', 'unixepoch'),";
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
		}
	}
?>