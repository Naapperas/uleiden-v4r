<?php
	class Base{
		// fill this array with the methods that you want publicly available
		// this is to make sure you're not accidentally making methods public when they shouldn't be
		protected $allowed = array();

		public function __construct(){
		}

		public function IsAllowed($method){
			if(in_array($method, $this->allowed))
				return true;
			else
				return false;
		}

		public function LastUpdate($id=null){
			if($id == null){
				return $this->query("SELECT hostupdate FROM game")[0]['hostupdate'];
			}
			else{
				return $this->query("SELECT lastupdate FROM student WHERE id=?", array($id))[0]['lastupdate'];
			}
		}
		public function microtime_float(){
		    list($usec, $sec) = explode(" ", microtime());
	    	return ((float)$usec + (float)$sec);
		}
	}

	
?>