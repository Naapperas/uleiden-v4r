<?php
	class Database extends Base{
		private $db_host;
		private $db_user;
		private $db_pass;
		private $db_name;
		private $conn;
		
		function __construct(){
			session_start();
			try{
				$this->db_host = 'localhost';
				$this->db_user = 'DB_USERNAME';
				$this->db_pass = 'DB_PASSWORD';
				$this->db_name = 'DB_DBNAME';

				if($this->conn == null)
					$this->conn = new PDO('mysql:host='.$this->db_host.';dbname='.$this->db_name, $this->db_user, $this->db_pass);
			}
			catch(PDOException $e){
				echo ($this->status == 'dev') ? $e->getMessage() : '';
			}
		}
		
		function __destruct(){
			$this->conn = null;
		}
		
		public function query($statement, $vars = null, $getid=null){
			try{
				$result = array();
				$query = $this->conn->prepare($statement);
				($vars != null) ? $query->execute($vars) : $query->execute();
				if($getid == true)
					return $this->conn->lastInsertID();
				$result = $query->fetchAll(PDO::FETCH_ASSOC);
				foreach($result as $key1=>$arr){
					foreach($arr as $key=>$var)
						$result[$key1][$key] = $var;
				}
				return $result;
			}
			catch (PDOException $e){
				echo ($this->status == 'dev') ? $e->getMessage() : '';
			}
		}

		
		public function display($array){
			echo "<pre>";
			print_r($array);
			echo "</pre>";
		}
	}

?>