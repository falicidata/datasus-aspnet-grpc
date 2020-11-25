# datasus-aspnet-grpc

Server/Porta levantado: http://45.79.41.194:8081

Exemplo de Request:

  params:
  
    modulo (banco) : CIHA
    
    meses:  1,2,3,4,5,6,7,8,9,10,11,12
    
    anos: 2020,2019
    
    campos: GESTAO,QT_PROC
    
    
 Request : http://45.79.41.194:8081/sus?meses=1,2,3,4,5,6,7,8,9,10,11,12&anos=2020&ufs=SP&modulo=CIHA&campos=GESTAO,QT_PROC
(request_exemplo.png)
