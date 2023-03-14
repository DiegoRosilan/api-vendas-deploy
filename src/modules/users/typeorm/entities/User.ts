import {
  Column,
  CreateDateColumn,
  Entity,
  PrimaryGeneratedColumn,
  UpdateDateColumn,
} from 'typeorm';
import { Exclude } from 'class-transformer';
import { Expose } from 'class-transformer';

@Entity('users')
class User {
  @PrimaryGeneratedColumn('uuid')
  id: string;

  @Column()
  name: string;

  @Column()
  email: string;

  @Column()
  //impede que retorne a senha
  @Exclude()
  password: string;

  @Column()
  avatar: string;

  @CreateDateColumn()
  created_at: Date;

  @UpdateDateColumn()
  updated_at: Date;

  //cria um campo virtual para pegar o caminho completo do Avatar
  @Expose({ name: 'avatar_url' })
  getAvatarURL(): string | null {
    if (!this.avatar) {
      return null;
    }
    //retorna o caminho atraves da junção do endereço da variável APP_API_URL
    //e o nome do arquvivo da tabela user
    return `${process.env.APP_API_URL}/files/${this.avatar}`;
  }
}

export default User;
