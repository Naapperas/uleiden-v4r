<?php
	class Database extends Base{
		private $conn;
		
		function __construct(){
			session_start();
			try{
				if($this->conn == null) {
					$this->conn = new PDO("sqlite:".dirname(__DIR__)."/db.db");
					$this->conn->setAttribute(PDO::ATTR_DEFAULT_FETCH_MODE, PDO::FETCH_ASSOC);
					$this->conn->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);
					$this->conn->exec("PRAGMA foreign_keys = ON");
				}
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