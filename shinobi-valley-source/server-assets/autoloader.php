<?php
	spl_autoload_register(function ($class_name) {
	    include_once "class/class." . strtolower($class_name) . '.php';
	});
?>