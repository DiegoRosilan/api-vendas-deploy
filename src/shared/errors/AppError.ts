class AppError {
  //variáveis somente leitura
  public readonly message: string;
  public readonly statusCode: number;

  //construtor da classe
  constructor(message: string, statusCode = 400) {
    this.message = message;
    this.statusCode = statusCode;
  }
}
export default AppError;
