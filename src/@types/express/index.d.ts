//declaração do name space da biblioteca que desejamos fazer o override
declare namespace Express {
  //metodo da bibliotreca que desejamos fazer o override
  export interface Request {
    //override
    user: {
      id: string;
    };
  }
}
