    <?php
    $host="eclipse.hopto.org"; 
    $port = 70; 
    $fp = fsockopen ($host, $port, $errno, $errstr); 
    if (!$fp) { $result = "Errore: impossibile confermare la registrazione, provare più tardi"; } 
    else {  
    $message = "CON/" . $_GET['id'];
    fputs($fp, $message); 
    fclose ($fp);
    $result = "Registrazione" . " " . "effettuata";
    } 
    echo $result;
    ?>